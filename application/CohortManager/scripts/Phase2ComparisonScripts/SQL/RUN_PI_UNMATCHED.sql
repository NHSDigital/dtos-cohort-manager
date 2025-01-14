create or replace procedure
	RUN_UNMATCHED_PI
	{
		insert into pi_unmatched
		(
			pi_change_id,
			nhs_number,
			date_of_birth,
			insert_date_time
		)
		select
			p.pi_change_id,
			p.nhs_number,
			p.date_of_birth,
			p.inserted_date_time,
		from
			pi_changes p
		left join
			ch_changes c
		on c.nhs_number = p.nhs_number
		and c.date_of_birth = p.date_of_birth
		where c.nhs_number is null

	}
