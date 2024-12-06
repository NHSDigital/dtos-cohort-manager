# Note: this works with schema
#   GP_PRACTICE_CODE VARCHAR(8),
#   BSO VARCHAR(4),
#   COUNTRY_CATEGORY VARCHAR(15),
#   AUDIT_ID NUMERIC(38),
#   AUDIT_CREATED_TIMESTAMP DATETIME,
#   AUDIT_LAST_MODIFIED_TIMESTAMP DATETIME,
#   AUDIT_TEXT VARCHAR(50)
   

# Issues:
#   1. Writes whole sql query when over 1000 items it breaks sql limits. (Fix: every 1000 lines add a new insert statement)
#   2. At the end of insert statement change last record to end with ; instead of ,

import csv
import datetime

def timestamp_conversion(time):
  date_str = time[0:19]
  format_str = '%d/%m/%Y %H:%M:%S' # The format
  datetime_obj = datetime.datetime.strptime(date_str, format_str)
  return datetime_obj  


filepath = input("Enter your file path: ")
with open(filepath, mode ='r')as input_file:
  csvFile = csv.reader(input_file)
  with open('bs_select_gp_practice_lpk.txt', mode = 'x') as output_file:
    headers = next(csvFile)
    for schema in csvFile:
        # Named variables for readability.
        gp_practice_code = schema[0]
        bso = schema[1]
        country_category = schema[2]
        audit_id = schema[3]
        aud_created = schema[4]
        audit_created_timestamp = timestamp_conversion(aud_created)
        aud_last_mod = schema[5]
        audit_last_modified_timestamp = timestamp_conversion(aud_last_mod)
        audit_text = schema[6]

        output_file.write('\t(\'' + gp_practice_code + '\', \'' + bso + '\', \'' + country_category + '\', \'' + audit_id + '\', \'' + str(audit_created_timestamp) 
              + '\', \'' + str(audit_last_modified_timestamp) + '\', \'' + audit_text +'\'), \n')
        
print('File created!')   