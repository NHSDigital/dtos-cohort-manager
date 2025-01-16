-- PROCEDURE: public.run_unmatched_cm(date)

-- DROP PROCEDURE IF EXISTS public.run_unmatched_cm(date);

CREATE OR REPLACE PROCEDURE public.run_unmatched_cm(
	IN rundateparam date)
LANGUAGE plpgsql
AS $BODY$
DECLARE
    row_count INT;
Begin
		insert into cm_unmatched
		(
			run_date,
			cm_change_id,
			nhs_number,
			date_of_birth,
			insert_date_time,
			execution_date_time
		)
		select
			rd.rundate,
			c.pi_change_id,
			c.nhs_number,
			c.date_of_birth,
			c.inserted_date_time,
			CURRENT_TIMESTAMP
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
		left join
				pi_changes p
			on
				c.nhs_number = p.nhs_number
			and
				c.date_of_birth = p.date_of_birth
			and
				rd.pi_start_date < c.inserted_date_time
			and
				rd.pi_end_date >= c.inserted_date_time

		where
			p.nhs_number is null;


	GET DIAGNOSTICS row_count = ROW_COUNT;
	update rundates
	set
		cm_unmatched = row_count
	where
		rundate = rundateparam;


end
$BODY$;

