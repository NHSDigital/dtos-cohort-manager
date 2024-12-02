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
import datetime
import pandas as pd
import numpy as np
import logging

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
    df['discrepancy_category_description'] = np.nan
    df['discrepancy_category_id'] = np.nan

    return df.apply(determine_discrepancy_category, axis=1)

def determine_discrepancy_category(row):
    age = calculate_age(row['date_of_birth'])
    within_cohort_age = age >= 44 and age < 74
    death_rfr = row['reason_for_removal'] in ['DEATH', 'DEA', 'UNCERTIFIED_DEATH']
    primary_care_provider = row['primary_care_provider']

    discrepancy_id = 0

    if within_cohort_age:
        if pd.isna(primary_care_provider):
            if death_rfr:
                discrepancy_id = 4
            else:
                discrepancy_id = 5
        elif primary_care_provider.startswith('ZZZ'):
            discrepancy_id = 6
        
        if row['gender'] == 'MALE' or row['gender'] == 1:
            discrepancy_id = 7
    elif age < 44 and 'is_higher_risk' in row:
        if row['is_higher_risk']:
            discrepancy_id = 1
        else:
            discrepancy_id = 2
    elif age >= 74:
        discrepancy_id = 3

    row['discrepancy_category_id'] = discrepancy_id
    row['discrepancy_category_description'] = discrepancy_map[discrepancy_id]

    return row



def calculate_age(date_of_birth):
    format = '%Y-%m-%d'
    if isinstance(date_of_birth, str):
        date_of_birth = datetime.datetime.strptime(date_of_birth, format)

    today = datetime.date.today()

    age = today.year - date_of_birth.year

    birthday_occurred = (today.month, today.day) >= (date_of_birth.month, date_of_birth.day)

    if not birthday_occurred:
        return age - 1
    
    return age

def preprocess_data(df):
    columns = ['nhs_number', 'date_of_birth', 'primary_care_provider',
                'reason_for_removal', 'reason_for_removal_business_effective_from_date',
                'date_of_death', 'postcode', 'gender']

    df.columns = df.columns.str.lower()
    df = df.fillna(np.nan).replace('', np.nan)

    if 'name_prefix' in df.columns:
        df = df[columns]

    return df

def get_unique_rows(caas_df, bss_df):
    merged_on_caas_df = pd.merge(caas_df, bss_df, on=["nhs_number", "date_of_birth"], how="left", suffixes=('', '_bss'), indicator=True)
    merged_on_bss_df = pd.merge(bss_df, caas_df, on=["nhs_number", "date_of_birth"], how="left", suffixes=('', '_caas'), indicator=True)

    # Drop rows and columns that are only in bss
    caas_only_df = merged_on_caas_df[merged_on_caas_df['_merge'] == 'left_only'].drop(columns=['_merge'])
    caas_only_df = caas_only_df.drop(columns=caas_only_df.filter(like='_bss').columns)

    bss_only_df = merged_on_bss_df[merged_on_bss_df['_merge'] == 'left_only'].drop(columns=['_merge'])
    bss_only_df = bss_only_df.drop(columns=bss_only_df.filter(like='_caas').columns)

    return caas_only_df, bss_only_df
