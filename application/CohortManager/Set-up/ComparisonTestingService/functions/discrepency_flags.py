"""
Rules:
    Vars:
        age = DateTime.Today - DOB

    1. People aged < 44 years of age who are flagged as High Risk          *ONLY ON BSS
        age < 44 yrs && is_higher_risk == True
    
    2. People aged < 44 years of age who are not flagged as High Risk      *ONLY ON BSS 
        Else rule 1.

    3. People aged >= 74 years of age
        age >= 74

    4. People aged >= 44 and < 74 who are not registered with a GP and have an RfR = D or DEA                           *BSS may have different values for RFR
        age >= 44 && age < 74 && primary_care_provider == null && (reason_for_removal == 'D' or reason_for_removal == 'DEA'

    5. People aged >= 44 and < 74 who not registered with a GP and have an RfR that is NOT = D' or 'DEA'                  *BSS may have different values for RFR
        age >= 44 && age < 74 && primary_care_provider == null && (reason_for_removal != 'D' and reason_for_removal != 'DEA'

    6. People aged >= 44 and < 74 who are registered with a dummy GP practice
        age >= 44 && age < 74 && primary_care_provider.StartsWith('ZZZ')

    7. People aged >= 44 and < 74 whose gender = male                     *CaaS has a gender code and BSS has a string
        age >= 44 && age < 74 && gender == 'MALE'
"""
from datetime import datetime, date
import pandas as pd
import numpy as np
from config import logger

discrepancy_map = {
    0: 'Discrepency category could not be determined',
    1: 'Below cohort age and high risk',
    2: 'Below cohort age and not high risk',
    3: 'Above cohort age',
    4: 'Within cohort age and not registered to a GP with death reason for removal',
    5: 'Within cohort age and not registered to a GP without death reason for removal',
    6: 'Within cohort age and registered to a dummy GP practice',
    7: 'Within cohort age and male'
}


def flag_discrepencies(df: pd.DataFrame):

    df = initialze_categorisation_columns(df)

    is_bss = 'is_higher_risk' in df.columns
    logger.info(is_bss)

    discrepancy_conditions = [
        (is_bss and df['age'] < 44), # and df['is_higher_risk'].bool()),
        # (is_bss and df['age'] < 44 and df['is_higher_risk'] != True),
        (df['age'] >= 74),
        # (df['eligible_age'] and df['primary_care_provider'].isna() and df['death_rfr']),
        # (df['eligible_age'] and df['primary_care_provider'].isna() and not df['death_rfr']),
        # (df['eligible_age'] and df['primary_care_provider'].str.startswith('ZZZ')),
        # (df['eligible_age'] and df['gender'] == 'MALE' or df['gender'] == 1)
    ]
    discrepancy_ids = [1, 2] #, 3, 4, 5, 6, 7]

    df['discrepancy_category_id'] = np.select(discrepancy_conditions, discrepancy_ids)

    # df.loc[df['age'] >= 74, 'discrepancy_category_id'] = 3
    # df.loc[df['eligible_age'] and df['primary_care_provider'].isna() and df['death_rfr'], 'discrepancy_category_id'] = 3

    # df = df.apply(determine_discrepancy_category, axis=1)

    df['discrepancy_category_description'] = df['discrepancy_category_id'].map(discrepancy_map)

    df = df.drop(columns=['age', 'eligible_age', 'death_rfr'])

    return df

def determine_discrepancy_category(row):
    discrepancy_id = 0

    if row['eligible_age']:
        if pd.isna(row['primary_care_provider']):
            if row['death_rfr']:
                discrepancy_id = 4
            else:
                discrepancy_id = 5
        elif row['primary_care_provider'].startswith('ZZZ'):
            discrepancy_id = 6
        
        if row['gender'] == 'MALE' or row['gender'] == 1:
            discrepancy_id = 7
    elif row['age'] < 44 and 'is_higher_risk' in row:
        if row['is_higher_risk']:
            discrepancy_id = 1
        else:
            discrepancy_id = 2
    # elif row['age'] >= 74:
    #     discrepancy_id = 3

    row['discrepancy_category_id'] = discrepancy_id

    return row



# def calculate_age(date_of_birth):
#     format = '%Y-%m-%d'
#     if isinstance(date_of_birth, str):
#         date_of_birth = datetime.datetime.strptime(date_of_birth, format)

#     today = datetime.date.today()

#     age = today.year - date_of_birth.year

#     birthday_occurred = (today.month, today.day) >= (date_of_birth.month, date_of_birth.day)

#     if not birthday_occurred:
#         return age - 1
    
#     return age

def initialze_categorisation_columns(df: pd.DataFrame):
    df['discrepancy_category_id'] = 0
    df['date_of_birth'] = pd.to_datetime(df['date_of_birth'])

    today = date.today()

    df['age'] = df['date_of_birth'].apply(
               lambda x: today.year - x.year - 
               ((today.month, today.day) < (x.month, x.day)))
    df['eligible_age'] = (df['age'] >= 44) & (df['age'] < 74)

    df['death_rfr'] = df['reason_for_removal'].isin(['DEATH', 'DEA', 'UNCERTIFIED_DEATH'])

    return df