# Note: this works with schema
#   OUTCODE VARCHAR(4) PRIMARY KEY,
#   BSO VARCHAR(4),
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
  with open('select_outcode_mapping_lkp_output.txt', mode = 'x') as output_file:
    headers = next(csvFile)
    for schema in csvFile:
        # Named variables for readability.
        outcode = schema[0]
        bso = schema[1]
        audit_id = schema[2]
        aud_created = schema[3]
        audit_created_timestamp = timestamp_conversion(aud_created)
        aud_last_mod = schema[4]
        audit_last_modified_timestamp = timestamp_conversion(aud_last_mod)
        audit_text = schema[5]

        output_file.write('\t(\'' + outcode + '\', \'' + bso + '\', \'' + audit_id + '\', \'' + str(audit_created_timestamp) 
              + '\', \'' + str(audit_last_modified_timestamp) + '\', \'' + audit_text +'\'), \n')
        
print('File created!')
