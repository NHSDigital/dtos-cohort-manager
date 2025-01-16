DELETE FROM ONLY ch_changes
WHERE pi_change_id IN (
    SELECT pi_change_id
    FROM ch_changes
    ORDER BY transaction_id
    LIMIT 100
);

create table rundates(
	rundate date unique primary key,
	pi_start_date timestamp  with time zone,
	pi_end_date timestamp with time zone,
	cm_start_date timestamp with time zone,
	cm_end_date timestamp with time zone
)

select inserted_date_time::date, count(*) from pi_changes
group by inserted_date_time::date
order by 1

DO $$
DECLARE
    start_rundate DATE := '2025-01-10';
    current_date DATE := CURRENT_DATE;
    target_date DATE;
BEGIN
    target_date := start_rundate; -- Initialize the target_date with the starting date
    WHILE target_date <= current_date LOOP
        INSERT INTO rundates (
            rundate,
            pi_start_date,
            pi_end_date,
            cm_start_date,
            cm_end_date
        )
        VALUES (
            target_date,
            target_date - INTERVAL '1 day' + INTERVAL '18 hours',
            target_date + INTERVAL '18 hours',
            target_date::timestamp,
            target_date + INTERVAL '1 day'
        )
        ON CONFLICT (rundate) DO NOTHING;
		target_date := target_date + INTERVAL '1 day';
    END LOOP;
END $$;

DO $$
DECLARE
    start_date TIMESTAMP := '2025-01-09 18:01:00'; -- Replace with your desired start date
    end_date TIMESTAMP := '2025-01-15 23:59:59'; -- Replace with your desired end date
BEGIN
    -- Update each timestamp column with a random value between start_date and end_date
    UPDATE ch_changes
    SET
        inserted_date_time = start_date + (random() * (end_date - start_date));
END $$;


select * from ch_changes

	call run_unmatched_pi('2025-01-13');
	call run_unmatched_cm('2025-01-13');
  call run_compare_attributes('2025-01-13');
	select * from cm_unmatched;
	select * from pi_unmatched;
	truncate table pi_unmatched;
	truncate table cm_unmatched;
