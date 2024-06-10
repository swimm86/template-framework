#!/bin/bash

print_changes() {
    echo "$2 $3" >> "$1"
    echo "$4 $5" >> "$1"
    echo "" >> "$1"
}

rename_directories() {
    find "$1" -type d | while read -r dirname; do
        if [[ "$(basename "$dirname")" = $2.* ]]; then
            new_dirname="${dirname//$2/$3}"
            mv "$dirname" "$new_dirname"
            print_changes "$4" "d-" "$(basename "$dirname")" "d>" "$(basename "$new_dirname")"
        fi
    done
}

replace_text_in_files() {
    find "$1" -type f \( -regex ".*\.\(sln\|csproj\|cs\)" -o -name "Dockerfile" \) | while read -r filename; do
        sed -i "s/$2/$3/g" "$filename"
        if [[ "$(basename "$filename")" = $2.* ]]; then
            new_filename="${filename//$2/$3}"
            mv "$filename" "$new_filename"
            print_changes "$4" "f-" "$(basename "$filename")" "f>" "$(basename "$new_filename")"
        fi
    done
}

script_dir=$(dirname "$(realpath "$0")")
log_file="$script_dir/log.txt"

> "$log_file"

read -r -p "Введите директорию, в которой находится .sln: " root_dir
if [ ! -d "$root_dir" ]; then
    echo "Указанная директория не существует"
    exit 1
fi

sln_files=($(find "$root_dir" -name "*.sln"))

if [ ${#sln_files[@]} -eq 0 ]; then
    echo "*.sln файл не найден."
    exit 1
fi

old_name=$(basename "${sln_files[0]}" .sln)
echo "Текущее название проекта: $old_name"

read -r -p "Хотите сменить его название (y/n)? " choice
if [ "$choice" != "y" ]; then
    exit 0
fi

read -r -p "Введите новое название для проекта: " new_name

rename_directories "$root_dir" "$old_name" "$new_name" "$log_file"
replace_text_in_files "$root_dir" "$old_name" "$new_name" "$log_file"