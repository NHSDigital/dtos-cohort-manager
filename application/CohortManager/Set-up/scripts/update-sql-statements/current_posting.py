# Note: this works with schema
#   POSTING VARCHAR(4) PRIMARY KEY,
#   IN_USE VARCHAR(1),
#   INCLUDED_IN_COHORT VARCHAR(1),
#   POSTING_CATEGORY VARCHAR(10)
   

# Issues:
#   1. Writes whole sql query when over 1000 items it breaks sql limits. (Fix: every 1000 lines add a new insert statement)
#   2. At the end of insert statement change last record to end with semi-colon (;) instead of a comma (,).

import csv 

filepath = input("Enter your file path: ")
with open(filepath, mode ='r') as input_file:
  csvFile = csv.reader(input_file)
  with open('current_posting.txt', mode = 'x') as output_file:
    headers = next(csvFile)
    for schema in csvFile:
        # Named variables for readability.
        posting = schema[0]
        in_use = schema[1]
        included_in_cohort = schema[2]
        posting_category = schema[3]

        output_file.write('\t(\'' + posting + '\', \'' + in_use + '\', \'' + included_in_cohort + '\', \'' + posting_category +'\'), \n')
        
print('File created!')   