create or replace procedure public.run_compare_attributes(
	IN rundateparam date
)
language plpgsql
as $body$
DECLARE
    row_count INT;
BEGIN
		insert into attribute_differences (
			run_date,
			pi_change_id,
			cm_change_id
		)
		select
			rundateparam,
			p.pi_change_id,
			c.pi_change_id
		from
			ch_changes c
		inner join
				rundates rd
			on
				rd.rundate = rundateparam
			and
				rd.cm_start_date < c.inserted_date_time
			and
				rd.cm_end_date >= c.inserted_date_time
		inner join
				pi_changes p
			on
				c.nhs_number = p.nhs_number
			and
				c.date_of_birth = p.date_of_birth
			and
				rd.pi_start_date < p.inserted_date_time
			and
				rd.pi_end_date >= p.inserted_date_time
		where
				c.message_id != p.message_id
			or
				c.name_prefix != p.name_prefix
			or
				c.family_name != p.family_name
			or
				c.first_name != p.first_name
			or
				c.other_names != p.other_names
			or
				c.previous_family_name != p.previous_family_name
			or
				c.date_of_death != p.date_of_death
			or
				c.gender_code != p.gender_code
			or
				c.address_line_1 != p.address_line_1
			or
				c.address_line_2 != p.address_line_2
			or
				c.address_line_3 != p.address_line_3
			or
				c.address_line_4 != p.address_line_4
			or
				c.address_line_5 != p.address_line_5
			or
				c.postcode != p.postcode
			or
				c.gp_practice_code != p.gp_practice_code
			or
				c.nhais_deduction_reason != p.nhais_deduction_reason
			or
				c.nhais_deduction_date != p.nhais_deduction_date
			or
				c.superseded_by_nhs_number != p.superseded_by_nhs_number
			or
				c.telephone_number_home != p.telephone_number_home
			or
				c.telephone_number_mobile != p.telephone_number_mobile
			or
				c.email_address_home != p.email_address_home
			or
				c.preferred_language != p.preferred_language
			or
				c.interpreter_required != p.interpreter_required
			or
				c.usual_address_eff_from_date != p.usual_address_eff_from_date
			or
				c.tel_number_home_eff_from_date != p.tel_number_home_eff_from_date
			or
				c.tel_number_mob_eff_from_date != p.tel_number_mob_eff_from_date
			or
				c.email_addr_home_eff_from_date != p.email_addr_home_eff_from_date;

	GET DIAGNOSTICS row_count = ROW_COUNT;

    RAISE NOTICE 'Rows affected: %', row_count;

END
$body$

