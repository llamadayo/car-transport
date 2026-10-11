-- Existing schemas only; new schemas already include TONS in 003_D02_D04.sql.
ALTER TABLE CARGO_D_RESOURCE ADD (TONS NUMBER(9,3));
COMMENT ON COLUMN CARGO_D_RESOURCE.TONS IS 'Vehicle tonnage for display hints only; not a dispatch restriction';
