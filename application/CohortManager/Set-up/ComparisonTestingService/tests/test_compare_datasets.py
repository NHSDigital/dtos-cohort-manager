import pytest
import pandas as pd
from src.discrepency_flags import get_unique_rows, calculate_age, flag_discrepencies
import pandas.testing as pdt
from datetime import datetime

def test_common_participants_removed():
    """
    Tests if the compare datasets function correctly removes the
    the participants that are in common between caas and bss
    """

    # Arrange
    caas_df = pd.DataFrame({
        'nhs_number': ['1111', '2222'],
        'date_of_birth': ['1934-12-11', '2045-12-11']
    })
    bss_df = pd.DataFrame({
        'nhs_number': ['1111', '3333'],
        'date_of_birth': ['1934-12-11', '1834-16-34']
    })

    expected_caas_df = pd.DataFrame({
        'nhs_number': ['2222'],
        'date_of_birth': ['2045-12-11']
    })
    expected_bss_df = pd.DataFrame({
        'nhs_number': ['3333'],
        'date_of_birth': ['1834-16-34']
    })

    # Act
    actual_caas_df, actual_bss_df = get_unique_rows(caas_df, bss_df) 

    actual_caas_df = actual_caas_df.reset_index(drop=True)
    actual_bss_df = actual_bss_df.reset_index(drop=True)

    # Assert
    pdt.assert_frame_equal(actual_caas_df, expected_caas_df)
    pdt.assert_frame_equal(actual_bss_df, expected_bss_df)

@pytest.mark.parametrize("dob, expected_age", [
    ("2000-06-13", 24),
    ("2000-12-31", 23),
    (datetime(2000, 6, 13), 24)])
def test_calculate_age(dob, expected_age):
    assert calculate_age(dob) == expected_age

def test_flag_discrepancy_not_determined():
    # Arrange
    input_df = pd.DataFrame({
        'nhs_number': ['0000'],
        'date_of_birth': ['1970-5-12'],
        'primary_care_provider': ['G82650'],
        'reason_for_removal': [None],
        'is_higher_risk': [False],
        'gender': 'FEMALE'
    })

    # Act
    actual_df = flag_discrepencies(input_df)

    # Assert
    assert actual_df['discrepancy_category_id'].iloc[0] == 0

def test_discrepancy_category_1():
    # Arrange
    input_df = pd.DataFrame({
        'nhs_number': ['1111'],
        'date_of_birth': ['2000-5-12'],
        'primary_care_provider': ['G82650'],
        'reason_for_removal': [None],
        'is_higher_risk': [True]
    })

    # Act
    actual_df = flag_discrepencies(input_df)

    assert actual_df['discrepancy_category_id'].iloc[0] == 1

def test_discrepancy_category_2():
    # Arrange
    input_df = pd.DataFrame({
        'nhs_number': ['2222'],
        'date_of_birth': ['2000-5-12'],
        'primary_care_provider': ['ZZZGB123d'],
        'reason_for_removal': [None],
        'is_higher_risk': [False]
    })

    # Act
    actual_df = flag_discrepencies(input_df)

    # Assert
    assert actual_df['discrepancy_category_id'].iloc[0] == 2

def test_discrepancy_category_3():
    # Arrange
    input_df = pd.DataFrame({
        'nhs_number': ['3333'],
        'date_of_birth': ['1923-5-12'],
        'primary_care_provider': ['G82650'],
        'reason_for_removal': [None],
    })

    actual_df = flag_discrepencies(input_df)

    assert actual_df['discrepancy_category_id'].iloc[0] == 3

def test_discrepancy_category_4():
    # Arrange
    input_df = pd.DataFrame({
        'nhs_number': ['4444'],
        'date_of_birth': ['1970-5-12'],
        'primary_care_provider': [None],
        'reason_for_removal': ['DEATH'],
        'gender': 'FEMALE'
    })

    # Act
    actual_df = flag_discrepencies(input_df)

    # Assert
    assert actual_df['discrepancy_category_id'].iloc[0] == 4

def test_discrepancy_category_5():
    # Arrange
    input_df = pd.DataFrame({
        'nhs_number': ['5555'],
        'date_of_birth': ['1970-5-12'],
        'primary_care_provider': [None],
        'reason_for_removal': ['NOT DEATH'],
        'gender': 'FEMALE'
    })

    # Act
    actual_df = flag_discrepencies(input_df)

    # Assert
    assert actual_df['discrepancy_category_id'].iloc[0] == 5

def test_discrepancy_category_6():
    # Arrange
    input_df = pd.DataFrame({
        'nhs_number': ['6666'],
        'date_of_birth': ['1970-5-12'],
        'primary_care_provider': ['ZZZG82650'],
        'reason_for_removal': [None],
        'gender': 'FEMALE'
    })

    # Act
    actual_df = flag_discrepencies(input_df)

    # Assert
    assert actual_df['discrepancy_category_id'].iloc[0] == 6

@pytest.mark.parametrize("gender", ["MALE", 1])
def test_discrepancy_category_7(gender):
    # Arrange
    input_df = pd.DataFrame({
        'nhs_number': ['7777'],
        'date_of_birth': ['1970-5-12'],
        'primary_care_provider': ['ZZZG82650'],
        'reason_for_removal': [None],
        'gender': gender
    })

    # Act
    actual_df = flag_discrepencies(input_df)

    # Assert
    assert actual_df['discrepancy_category_id'].iloc[0] == 7