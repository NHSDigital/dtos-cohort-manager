from datetime import date
import pandas as pd
import numpy as np

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

    discrepancy_ids = [3, 4, 5, 6, 7]
    discrepancy_conditions = [
        (df['age'] >= 74),
        (df['eligible_age'] & df['primary_care_provider'].isna() & df['death_rfr']),
        (df['eligible_age'] & df['primary_care_provider'].isna() & ~df['reason_for_removal'].isna() & ~df['death_rfr']),
        (df['eligible_age'] & df['primary_care_provider'].str.startswith('ZZZ')),
        (df['eligible_age'] & ((df['gender'] == 'MALE') | (df['gender'] == 1)))
    ]

    if "is_higher_risk" in df.columns:
        df['is_higher_risk'] = df['is_higher_risk'].fillna(False)
        discrepancy_conditions.append(((df['age'] < 44) & df['is_higher_risk']))
        discrepancy_conditions.append(((df['age'] < 44) & ~df['is_higher_risk']))
        discrepancy_ids.append(1)
        discrepancy_ids.append(2)

    df['discrepancy_category_id'] = np.select(discrepancy_conditions, discrepancy_ids)
    df['discrepancy_category_description'] = df['discrepancy_category_id'].map(discrepancy_map)

    df = df.drop(columns=['age', 'eligible_age', 'death_rfr'])

    return df

def initialze_categorisation_columns(df: pd.DataFrame):
    df['discrepancy_category_id'] = 0
    df['date_of_birth'] = pd.to_datetime(df['date_of_birth'])

    today = date.today()

    df['age'] = df['date_of_birth'].apply(
               lambda x: today.year - x.year - 
               ((today.month, today.day) < (x.month, x.day)))
    df['eligible_age'] = (df['age'] >= 44) & (df['age'] < 74)

    df['death_rfr'] = (df['reason_for_removal'].isin(['DEATH', 'DEA', 'UNCERTIFIED_DEATH']))

    return df
