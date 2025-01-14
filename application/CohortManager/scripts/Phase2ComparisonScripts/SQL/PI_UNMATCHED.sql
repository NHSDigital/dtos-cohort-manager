Create table pi_unmatched
(
	pi_change_id integer not null,
	nhs_number character varying(10) COLLATE pg_catalog."default",
	date_of_birth date,
	insert_date_time date

)
Create table cm_unmatched
(
	cm_change_id integer not null,
	nhs_number character varying(10) COLLATE pg_catalog."default",
	date_of_birth date,
	insert_date_time date

)
