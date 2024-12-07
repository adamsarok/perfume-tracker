
import openpyxl
import requests
import json

# API endpoint
API_URL = "https://localhost:7139/api/Perfumes"  # Replace with your actual API URL

# Path to the Excel file
EXCEL_FILE = "c:\\Temp\\perfume-import.xlsx"  # Update with your file path

# Read Excel file
def read_excel(file_path):
    workbook = openpyxl.load_workbook(file_path)
    sheet = workbook.active

    perfumes = []
    headers = [cell.value for cell in sheet[1]]

    for row in sheet.iter_rows(min_row=2, values_only=True):
        perfume_data = dict(zip(headers, row))
        perfumes.append(perfume_data)
    
    return perfumes

# Upload data to the API
def upload_to_api(perfume):
    try:
        response = requests.post(API_URL, 
                                 json=perfume,
                                 verify=False  #test
                                 )
        response.raise_for_status()
        print(f"Uploaded: {perfume['perfumeName']} - Status Code: {response.status_code}")
    except requests.exceptions.RequestException as e:
        print(f"Error uploading {perfume['perfumeName']}: {e}")

def is_valid_data(perfume):
    return all(perfume.get(field) for field in ["house", "perfumeName", "notes"])

def main():
    perfumes = read_excel(EXCEL_FILE)
    for perfume in perfumes:
        perfume_payload = {
            "house": perfume.get("house"),
            "perfumeName": perfume.get("perfume"),
            "rating": float(perfume.get("rating") if perfume.get("rating") is not None else 1),
            "notes": perfume.get("notes", ""),
            "ml": 0
        }
        if is_valid_data(perfume_payload):
            upload_to_api(perfume_payload)
        else:
            print(f"Skipped: {perfume_payload['perfumeName']} due to missing string fields")

if __name__ == "__main__":
    main()
