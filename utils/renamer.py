import os
import sys

def rename_files_and_replace_text(root_dir, old_name, new_name, log_file):
    for dirpath, dirnames, filenames in os.walk(root_dir):
        for dirname in dirnames:
            if dirname.startswith(f"{old_name}."):
                rename_directory(dirpath, dirname, old_name, new_name, log_file)
                
    for dirpath, dirnames, filenames in os.walk(root_dir):
        for filename in filenames:
            if filename.endswith((".sln", ".csproj", ".cs")):
                replace_text_in_file(dirpath, filename, old_name, new_name)
                if filename.startswith(f"{old_name}."):
                    rename_file(dirpath, filename, old_name, new_name, log_file)

def rename_directory(dirpath, dirname, old_name, new_name, log_file):
    new_dirname = dirname.replace(old_name, new_name)
    os.rename(os.path.join(dirpath, dirname), os.path.join(dirpath, new_dirname))
    print_changes(log_file, "d-", dirname, "d>", new_dirname)

def rename_file(dirpath, filename, old_name, new_name, log_file):
    new_filename = filename.replace(old_name, new_name)
    os.rename(os.path.join(dirpath, filename), os.path.join(dirpath, new_filename))
    print_changes(log_file, "f-", filename, "f>", new_filename)

def replace_text_in_file(dirpath, filename, old_name, new_name):
    file_path = os.path.join(dirpath, filename)
    with open(file_path, 'r', encoding='utf-8') as file:
        file_data = file.read()
    new_file_data = file_data.replace(old_name, new_name)
    with open(file_path, 'w', encoding='utf-8') as file:
        file.write(new_file_data)

def print_changes(log_file, prefix_old, old, prefix_new, new):
    with open(log_file, 'a', encoding='utf-8') as f:
        f.write(f"{prefix_old}{old}\n")
        f.write(f"{prefix_new}{new}\n\n")

def get_user_input(prompt, validator=lambda value: True):
    value = input(prompt)
    while not validator(value):
        print("Введите корректное значение")
        value = input(prompt)
    return value

def main():
    script_dir = os.path.dirname(os.path.realpath(__file__))
    log_file = os.path.join(script_dir, 'log.txt')

    # Clear the log file at the start of the script
    open(log_file, 'w').close()

    root_dir = get_user_input("Введите директорию, в которой находится .sln:", os.path.isdir)
    sln_files = [file for file in os.listdir(root_dir) if file.endswith(".sln")]

    if not sln_files:
        print("*.sln файл не найден.")
        sys.exit(1)

    old_name = os.path.splitext(sln_files[0])[0]
    print("Текущее название проекта:", old_name)

    if get_user_input("Хотите сменить его название (y/n)?", lambda v: v.lower() in ('y', 'n')) == "n":
        sys.exit(0)

    new_name = get_user_input("Введите новое название для проекта:")
    rename_files_and_replace_text(root_dir, old_name, new_name, log_file)

if __name__ == "__main__":
    main()