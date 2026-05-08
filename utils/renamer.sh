#!/bin/bash
#
# rename-dotnet-project.sh
#
# Утилита массового переименования .NET/C# решения (solution).
#
# ОПИСАНИЕ:
#   Скрипт выполняет полное переименование .NET проекта, включая:
#     - Переименование директорий, имя которых совпадает с текущим именем
#       проекта (точное совпадение или с точкой-суффиксом: OldName, OldName.Core)
#     - Замену имени проекта в содержимом файлов (src, конфиги, документация)
#     - Переименование файлов (sln, csproj и др.), чьё имя совпадает с проектом
#     - Сортировку using-директив в .cs файлах согласно StyleCop правилам:
#         * System.* — первыми, по иерархии (System перед System.Linq)
#         * Остальные  — после, по иерархии
#         * Между группами — пустая строка-разделитель
#
# ИСПОЛЬЗОВАНИЕ:
#   chmod +x rename-dotnet-project.sh
#   ./rename-dotnet-project.sh
#
# БЕЗОПАСНОСТЬ:
#   Перед выполнением массовых изменений скрипт запрашивает подтверждение.
#   Все переименования логируются в log.txt рядом со скриптом.
#   При отмене на любом этапе скрипт завершается без изменений.
#
# ПОДДЕРЖИВАЕМЫЕ ФОРМАТЫ ФАЙЛОВ:
#   Исходный код:        .cs, .sln, .csproj
#   Конфигурация:        .json, .xml, .yaml, .yml, .config, .props, .targets
#   Шаблоны и разметка:  .xaml, .cshtml, .razor, .proto, .md, .editorconfig
#   Контейнеризация:     Dockerfile, docker-compose*, .tf, .tfvars
#   Скрипты:             .sh, Makefile
#
# ТРЕБОВАНИЯ:
#   - Bash 4.0+
#   - GNU awk, GNU sed, GNU grep, GNU sort, GNU find
#
# =============================================================================

set -euo pipefail

# =============================================================================
# Утилиты
# =============================================================================

usage() {
    cat <<EOF
Использование: $(basename "$0")

Утилита интерактивного переименования .NET/C# решения.
Запустите скрипт и следуйте подсказкам.

Опции:
  -h, --help    Показать эту справку
EOF
    exit 0
}

print_changes() {
    local log_file="$1"
    echo "$2 $3" >> "$log_file"
    echo "$4 $5" >> "$log_file"
    echo "" >> "$log_file"
}

# Экранирует спецсимволы для sed-паттерна (левая часть s/.../.../)
# Точка, звёздочка, скобки и прочее больше не интерпретируются как regex-метасимволы.
escape_sed_pattern() {
    printf '%s\n' "$1" | sed -e 's/\\/\\\\/g' -e 's/[.[\]*^$()+?{|]/\\&/g'
}

# Экранирует спецсимволы для sed-замены (правая часть s/.../.../)
# В части замены особые: & (вставка совпадения), \ (экранирование), / (разделитель).
escape_sed_replacement() {
    printf '%s\n' "$1" | sed -e 's/\\/\\\\/g' -e 's/[&/]/\\&/g'
}

# =============================================================================
# Основные функции
# =============================================================================

# Переименовывает директории, имя которых совпадает с old_name или начинается
# с old_name. (точка + суффикс).
#
# Использует find -depth для обхода от самых глубоких папок к корневым.
# Без -depth переименование родителя сделало бы дочерние пути недействительными.
#
# ПОСЛЕ ВЫПОЛНЕНИЯ: вызывающая сторона должна проверить, не был ли переименован
# сам root_dir, и при необходимости обновить путь (см. main()).
#
# Аргументы:
#   $1 — корневая директория для поиска
#   $2 — старое имя проекта
#   $3 — новое имя проекта
#   $4 — путь к файлу лога
rename_directories() {
    local root_dir="$1"
    local old_name="$2"
    local new_name="$3"
    local log_file="$4"

    # ЭТАП 1: собираем ВСЕ директории в массив до начала переименований.
    # Это критично: если запустить mv внутри find|while, find продолжит
    # обход файловой системы и может сгенерировать устаревшие/мусорные пути.
    local -a dirs=()
    while IFS= read -r -d '' dirname; do
        dirs+=("$dirname")
    done < <(find "$root_dir" -depth -type d -print0)

    # ЭТАП 2: обрабатываем собранные пути (find уже завершён).
    local dirname
    for dirname in "${dirs[@]}"; do
        # Пропускаем директории, которые перестали существовать
        # (могли быть перемещены вместе с родителем на предыдущих итерациях)
        [ ! -d "$dirname" ] && continue

        local base
        base=$(basename "$dirname")

        # Точное совпадение (например, корневая папка OldName)
        # ИЛИ совпадение по префиксу с точкой (например, OldName.Core)
        if [[ "$base" == "$old_name" || "$base" == "$old_name".* ]]; then
            local parent new_dirname
            parent=$(dirname "$dirname")
            new_dirname="$parent/${base//"$old_name"/"$new_name"}"

            # Защита: если целевой путь уже существует (каталог с таким именем),
            # mv переместит source ВНУТРЬ существующего каталога вместо переименования.
            if [ -d "$new_dirname" ]; then
                echo "Ошибка: директория уже существует: $new_dirname" >&2
                echo "  (источник: $dirname)" >&2
                exit 1
            fi

            mv "$dirname" "$new_dirname"
            print_changes "$log_file" "d-" "$base" "d>" "$(basename "$new_dirname")"
        fi
    done
}

# Выполняет замену текста внутри файлов и переименовывает файлы,
# чьё имя начинается со старого имени проекта.
#
# Экранирует спецсимволы в имени проекта для корректной работы sed.
# Обрабатывает широкий набор расширений, характерных для .NET проектов.
#
# Аргументы:
#   $1 — корневая директория для поиска (уже обновлённая после rename_directories)
#   $2 — старое имя проекта (буквальная строка, будет экранирована)
#   $3 — новое имя проекта (буквальная строка, будет экранирована)
#   $4 — путь к файлу лога
replace_text_in_files() {
    local root_dir="$1"
    local old_name="$2"
    local new_name="$3"
    local log_file="$4"

    local escaped_old escaped_new
    escaped_old=$(escape_sed_pattern "$old_name")
    escaped_new=$(escape_sed_replacement "$new_name")

    # ЭТАП 1: собираем ВСЕ файлы в массив до начала обработки.
    # Аналогично rename_directories — избегаем race condition с find.
    local -a files=()
    while IFS= read -r -d '' filename; do
        files+=("$filename")
    done < <(find "$root_dir" -type f \( \
        -regex '.*\.\(sln\|csproj\|cs\|json\|xml\|yaml\|yml\|xaml\|config\|props\|targets\|editorconfig\|cshtml\|razor\|proto\|md\)' \
        -o -name "Dockerfile" \
        -o -name "docker-compose*" \
        -o -name "Makefile" \
        -o -name "*.sh" \
        -o -name "*.tf" \
        -o -name "*.tfvars" \
    \) -print0)

    # ЭТАП 2: обрабатываем собранные файлы
    local filename
    for filename in "${files[@]}"; do
        # Пропускаем файлы, которые перестали существовать
        [ ! -f "$filename" ] && continue

        # Замена текста внутри файла
        sed -i "s/${escaped_old}/${escaped_new}/g" "$filename"

        # Переименование файла, если его имя начинается со старого проекта
        local base
        base=$(basename "$filename")
        if [[ "$base" == "$old_name".* ]]; then
            local dir new_filename
            dir=$(dirname "$filename")
            new_filename="$dir/${base//"$old_name"/"$new_name"}"
            mv "$filename" "$new_filename"
            print_changes "$log_file" "f-" "$base" "f>" "$(basename "$new_filename")"
        fi
    done
}

# Иерархическая сортировка using-директив в .cs файлах.
#
# Правила сортировки (соответствуют StyleCop SA1208 / SA1210 / SA1211):
#   1. System.* директивы — первыми, включая bare «using System;»
#   2. Остальные обычные using — после System, по иерархии
#   3. Alias using (using X = Y;) — последними, по имени алиаса
#   4. Внутри каждой группы — иерархическая сортировка по компонентам namespace,
#      разделённым точкой. Родительский namespace всегда идёт перед дочерним:
#        System перед System.Linq
#        System.Text.Json перед System.Text.Json.Serialization
#   5. Между группами — пустая строка-разделитель (если обе непустые)
#   6. Перед кодом — пустая строка-разделитель
#
# Поддерживаются: using, global using, using static, using alias.
# Дубликаты удаляются.
# Заголовок файла (комментарии, license) сохраняется без изменений.
#
# Аргументы:
#   $1 — корневая директория для поиска .cs файлов
sort_usings_in_cs_files() {
    local root_dir="$1"

    find "$root_dir" -type f -name "*.cs" -print0 | while IFS= read -r -d '' filename; do

        local first_using code_start

        first_using=$(awk '
            /^using[ \t]+/ || /^global[ \t]+using[ \t]+/ { print NR; exit }
        ' "$filename")
        [ -z "$first_using" ] && continue

        code_start=$(awk -v s="$first_using" '
            (/^using[ \t]+/ || /^global[ \t]+using[ \t]+/) && NR >= s { last = NR; next }
            /^[[:space:]]*$/ && last > 0 { next }
            { if (last > 0) { print NR; exit } }
        ' "$filename")
        [ -z "$code_start" ] && continue

        local tmpfile
        tmpfile=$(mktemp)
        trap 'rm -f "$tmpfile"' RETURN

        # 1. Заголовок файла (всё до первой using-директивы)
        if [ "$first_using" -gt 1 ]; then
            head -n $((first_using - 1)) "$filename" > "$tmpfile"
        fi

        # 2. Собираем все using-директивы из блока
        local usings
        usings=$(awk -v s="$first_using" -v e="$code_start" '
            (/^using[ \t]+/ || /^global[ \t]+using[ \t]+/) && NR >= s && NR < e { print }
        ' "$filename")

        # 3. Разделяем на три группы:
        #    a) System.* — обычные using для System-пространств (без alias)
        #    b) Other    — обычные using для остальных пространств (без alias)
        #    c) Alias    — using X = Y; (alias-директивы)
        local system_usings other_usings alias_usings
        system_usings=$(printf '%s\n' "$usings" | grep -E '^(using|global[[:space:]]+using|using[[:space:]]+static)[[:space:]]+System[.;]' | grep -vE '[[:space:]]+=' || true)
        alias_usings=$(printf '%s\n' "$usings" | grep -E '[[:space:]]+=' || true)
        other_usings=$(printf '%s\n' "$usings" | grep -vE '^(using|global[[:space:]]+using|using[[:space:]]+static)[[:space:]]+System[.;]' | grep -vE '[[:space:]]+=' || true)

        # 4. Иерархическая сортировка каждой группы через awk
        #    Сравнение по компонентам namespace (split по «.»):
        #    - одинаковые компоненты → сравниваем следующие
        #    - все совпали, но длины разные → короче идёт первым
        #    Для alias-группы ключ сортировки — имя алиаса (левая часть от «=»)
        local sorted_system sorted_other sorted_alias

        # AWK-функция иерархической сортировки; извлекается как общий шаблон
        local _awk_sort_ns='
            BEGIN { n = 0 }
            /^[[:space:]]*$/ { next }
            {
                lines[n] = $0
                ns = $0
                sub(/^global[ \t]+/, "", ns)
                sub(/^using[ \t]+static[ \t]+/, "", ns)
                sub(/^using[ \t]+/, "", ns)
                sub(/;.*$/, "", ns)
                keys[n] = ns
                n++
            }
            END {
                for (i = 0; i < n - 1; i++) {
                    for (j = i + 1; j < n; j++) {
                        split(keys[i], a, ".")
                        split(keys[j], b, ".")
                        if (ns_cmp(a, b) > 0) {
                            tmp = lines[i]; lines[i] = lines[j]; lines[j] = tmp
                            tmp = keys[i]; keys[i] = keys[j]; keys[j] = tmp
                        }
                    }
                }
                for (i = 0; i < n; i++) print lines[i]
            }
            function ns_cmp(a, b,    i, na, nb) {
                na = length(a); nb = length(b)
                for (i = 1; i <= na && i <= nb; i++) {
                    if (a[i] < b[i]) return -1
                    if (a[i] > b[i]) return 1
                }
                if (na < nb) return -1
                if (na > nb) return 1
                return 0
            }'

        # AWK-функция сортировки alias по имени алиаса (левая часть от «=»)
        local _awk_sort_alias='
            BEGIN { n = 0 }
            /^[[:space:]]*$/ { next }
            {
                lines[n] = $0
                ns = $0
                sub(/^using[ \t]+/, "", ns)
                sub(/[[:space:]]*=.*$/, "", ns)
                keys[n] = ns
                n++
            }
            END {
                for (i = 0; i < n - 1; i++) {
                    for (j = i + 1; j < n; j++) {
                        if (keys[i] > keys[j]) {
                            tmp = lines[i]; lines[i] = lines[j]; lines[j] = tmp
                            tmp = keys[i]; keys[i] = keys[j]; keys[j] = tmp
                        }
                    }
                }
                for (i = 0; i < n; i++) print lines[i]
            }'

        sorted_system=$(printf '%s\n' "$system_usings" | awk "$_awk_sort_ns")
        sorted_other=$(printf '%s\n' "$other_usings" | awk "$_awk_sort_ns")
        sorted_alias=$(printf '%s\n' "$alias_usings" | awk "$_awk_sort_alias")

        # 5. Записываем группы: System + Other идут подряд без разделителя,
        #    пустая строка — только перед Alias-группой
        if [ -n "$sorted_system" ]; then
            printf '%s\n' "$sorted_system" >> "$tmpfile"
        fi
        if [ -n "$sorted_other" ]; then
            printf '%s\n' "$sorted_other" >> "$tmpfile"
        fi
        if [ -n "$sorted_alias" ] && { [ -n "$sorted_system" ] || [ -n "$sorted_other" ]; }; then
            echo "" >> "$tmpfile"
        fi
        if [ -n "$sorted_alias" ]; then
            printf '%s\n' "$sorted_alias" >> "$tmpfile"
        fi

        # 6. Пустая строка-разделитель перед кодом
        echo "" >> "$tmpfile"

        # 7. Остальная часть файла (namespace, классы и т.д.)
        tail -n +"$code_start" "$filename" >> "$tmpfile"

        mv "$tmpfile" "$filename"
    done
}

# =============================================================================
# Основная логика
# =============================================================================

main() {
    if [[ $# -gt 0 ]]; then
        case "$1" in
            -h|--help) usage ;;
            *)
                echo "Неизвестный аргумент: $1" >&2
                usage
                ;;
        esac
    fi

    local script_dir log_file
    script_dir=$(dirname "$(realpath "$0")")
    log_file="$script_dir/log.txt"

    > "$log_file"

    echo "=== Renamer .NET Project ==="
    echo ""

    # --- Ввод директории с .sln ---
    local root_dir
    read -r -p "Введите директорию, в которой находится .sln: " root_dir
    if [ ! -d "$root_dir" ]; then
        echo "Ошибка: указанная директория не существует: $root_dir" >&2
        exit 1
    fi

    # --- Поиск .sln файлов ---
    local -a sln_files
    while IFS= read -r -d '' sln; do
        sln_files+=("$sln")
    done < <(find "$root_dir" -maxdepth 2 -name "*.sln" -print0)

    if [ ${#sln_files[@]} -eq 0 ]; then
        echo "Ошибка: *.sln файл не найден в $root_dir" >&2
        exit 1
    fi

    # --- Выбор решения (если несколько) ---
    local selected
    if [ ${#sln_files[@]} -eq 1 ]; then
        selected="${sln_files[0]}"
    else
        echo "Найдено несколько .sln файлов:"
        for i in "${!sln_files[@]}"; do
            echo "  [$i] ${sln_files[$i]}"
        done
        read -r -p "Выберите номер нужного: " choice_idx
        if [[ ! "$choice_idx" =~ ^[0-9]+$ ]] || [ "$choice_idx" -ge ${#sln_files[@]} ]; then
            echo "Ошибка: неверный выбор" >&2
            exit 1
        fi
        selected="${sln_files[$choice_idx]}"
    fi

    local old_name
    old_name=$(basename "$selected" .sln)
    echo ""
    echo "Текущее название проекта: $old_name"

    read -r -p "Хотите сменить его название (y/n)? " choice
    if [[ "$choice" != "y" ]]; then
        echo "Отменено."
        exit 0
    fi

    local new_name
    read -r -p "Введите новое название для проекта: " new_name
    if [ -z "$new_name" ]; then
        echo "Ошибка: имя не может быть пустым" >&2
        exit 1
    fi

    if [[ "$new_name" =~ [^a-zA-Z0-9._-] ]]; then
        echo "Ошибка: имя содержит недопустимые символы. Допускаются только латиница, цифры, точка, дефис и подчёркивание." >&2
        exit 1
    fi

    # --- Итоговое подтверждение ---
    echo ""
    echo "  Старое имя: $old_name"
    echo "  Новое имя: $new_name"
    echo "  Директория: $root_dir"
    echo ""
    read -r -p "Подтвердите выполнение (y/n)? " confirm
    if [[ "$confirm" != "y" ]]; then
        echo "Отменено."
        exit 0
    fi

    # --- Выполнение ---
    echo ""
    echo "[1/4] Переименование директорий..."
    rename_directories "$root_dir" "$old_name" "$new_name" "$log_file"

    # После переименования директорий root_dir мог перестать существовать,
    # если его имя совпало с old_name. Обновляем путь.
    if [ ! -d "$root_dir" ]; then
        local parent_dir
        parent_dir=$(dirname "$root_dir")
        root_dir="$parent_dir/$new_name"
        echo "  Корневая директория переименована: $root_dir"
    fi

    echo "[2/4] Замена текста и переименование файлов..."
    replace_text_in_files "$root_dir" "$old_name" "$new_name" "$log_file"

    echo "[3/4] Сортировка using-директив в .cs файлах..."
    sort_usings_in_cs_files "$root_dir"

    echo "[4/4] Готово!"
    echo ""
    echo "Лог изменений сохранён в: $log_file"
}

main "$@"
