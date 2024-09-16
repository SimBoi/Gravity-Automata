import json


def extract_hyperparameters(input_file_path):
    try:
        with open(input_file_path, 'r') as file:
            first_line = file.readline().strip()  # Read the first line
            hyperparameters = {}
            for param in first_line.split(','):
                key, value = param.split('=')
                hyperparameters[key] = float(value)
            return hyperparameters
    except FileNotFoundError:
        return None

# iterate over the files
all_hyperparameters = []
for i in range(0, 66):
    input_file_path = f"{i}.txt"
    hyperparams = extract_hyperparameters(input_file_path)
    if hyperparams:
        print(f"File: {input_file_path}, Hyperparameters: {hyperparams}")
        all_hyperparameters.append(hyperparams)
    else:
        print(f"File not found: {input_file_path}")

# save the hyperparameters to a file, as a JSON object
with open('hyperparameters.txt', 'w') as file:
    json.dump(all_hyperparameters, file)