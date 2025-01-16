-- PROCEDURE: public.run_unmatched_pi(date)

-- DROP PROCEDURE IF EXISTS public.run_unmatched_pi(date);

CREATE OR REPLACE PROCEDURE public.run_unmatched_pi(
	IN rundateparam date)
LANGUAGE plpgsql
AS $BODY$
DECLARE
    row_count INT;
BEGIN
		insert into pi_unmatched
		(
			run_date,
			pi_change_id,
			nhs_number,
			date_of_birth,
			insert_date_time,
			execution_date_time
		)
		select
			rd.rundate,
			p.pi_change_id,
			p.nhs_number,
			p.date_of_birth,
			p.inserted_date_time,
			CURRENT_TIMESTAMP
		from
			pi_changes p
		inner join
				rundates rd
			on
				rd.rundate = rundateparam
			and
				rd.pi_start_date < p.inserted_date_time
			and
				rd.pi_end_date >= p.inserted_date_time
		left join
				ch_changes c
			on
				c.nhs_number = p.nhs_number
			and
				c.date_of_birth = p.date_of_birth
			and
				rd.cm_start_date < c.inserted_date_time
			and
				rd.cm_end_date >= c.inserted_date_time

		where
			c.nhs_number is null;

		GET DIAGNOSTICS row_count = ROW_COUNT;

		update rundates
		set
			pi_unmatched = row_count
		where
			rundate = rundateparam;
END
$BODY$;

