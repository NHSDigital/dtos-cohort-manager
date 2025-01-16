Create table pi_unmatched
(
  run_date date not null,
	pi_change_id integer not null,
	nhs_number character varying(10) COLLATE pg_catalog."default",
	date_of_birth date,
	insert_date_time date,
  execution_date_time timestamp with time zone

)
Create table cm_unmatched
(
  run_date date not null,
	cm_change_id integer not null,
	nhs_number character varying(10) COLLATE pg_catalog."default",
	date_of_birth date,
	insert_date_time date,
  execution_date_time timestamp with time zone

)
CREATE TABLE IF NOT EXISTS public.rundates
(
    rundate date NOT NULL,
    pi_start_date timestamp with time zone,
    pi_end_date timestamp with time zone,
    cm_start_date timestamp with time zone,
    cm_end_date timestamp with time zone,
	pi_unmatched integer null,
	cm_unmatched integer null,
	attribute_differences integer null,
	matched_records integer null,
    CONSTRAINT rundates_pkey PRIMARY KEY (rundate)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.rundates
    OWNER to postgres;
