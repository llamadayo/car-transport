"""Generate both dialects from checked-in fictional seed data; no real company data."""
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
db = json.loads((ROOT / 'database/seed.json').read_text())
# Portable logical types are expanded explicitly for each database.
tables = {}
def table(name, cols, constraints=()):
    tables[name] = (cols, list(constraints))
table('VD_SCHEMA_VERSION', [('VERSION_NO','int','PRIMARY KEY')])
table('VD_COUNTER', [('KEY_NAME','id','PRIMARY KEY'),('NEXT_VAL','int','NOT NULL')])
table('VD_SITE', [('ID','id','PRIMARY KEY'),('NAME','text','NOT NULL'),('SORT_ORDER','int','NOT NULL UNIQUE'),('LATE_RESTRICTED','int','NOT NULL'),('RETURN_LOAD_BY','short','')])
table('VD_STATION', [('ID','id','PRIMARY KEY'),('BRANCH_ID','id','NOT NULL REFERENCES VD_SITE(ID)'),('NAME','text','NOT NULL'),('SORT_ORDER','int','NOT NULL')], ['UNIQUE (ID, BRANCH_ID)','UNIQUE (BRANCH_ID, SORT_ORDER)'])
table('VD_BUILDING', [('ID','id','PRIMARY KEY'),('SITE_ID','id','NOT NULL REFERENCES VD_SITE(ID)'),('STATION_ID','id',''),('NAME','text','NOT NULL')], ['FOREIGN KEY (STATION_ID, SITE_ID) REFERENCES VD_STATION(ID, BRANCH_ID)'])
table('VD_VEHICLE', [('ID','id','PRIMARY KEY'),('NAME','text','NOT NULL'),('POOL','short','NOT NULL'),('SIZE_CLASS','short',''),('HOME_SITE','id','REFERENCES VD_SITE(ID)'),('CURRENT_SITE','id','REFERENCES VD_SITE(ID)'),('LENGTH_CM','real','NOT NULL'),('WIDTH_CM','real','NOT NULL'),('HEIGHT_CM','real','NOT NULL'),('VOLUME','real','NOT NULL'),('WEIGHT_LIMIT','real','NOT NULL'),('SEATS','int','NOT NULL'),('SORT_ORDER','int','NOT NULL')], ["CHECK (POOL IN ('LOGI','BIZ'))"])
table('VD_DRIVER', [('ID','id','PRIMARY KEY'),('NAME','text','NOT NULL'),('POOL','short','NOT NULL'),('HOME_SITE','id','REFERENCES VD_SITE(ID)'),('CURRENT_SITE','id','REFERENCES VD_SITE(ID)'),('SORT_ORDER','int','NOT NULL')])
table('VD_CARGO_CATEGORY', [('CODE','id','PRIMARY KEY'),('NAME','text','NOT NULL'),('FACTOR','real','NOT NULL CHECK (FACTOR > 0)'),('ACTIVE','int','NOT NULL CHECK (ACTIVE IN (0,1))'),('SORT_ORDER','int','NOT NULL')])
table('VD_CONFIG', [('KEY_NAME','id','PRIMARY KEY'),('NUM_VALUE','real','NOT NULL')])
table('VD_REG_SHIFT', [('ID','id','PRIMARY KEY'),('BRANCH_ID','id','NOT NULL REFERENCES VD_SITE(ID)'),('LABEL','text','NOT NULL'),('DEPART_MINUTE','int','NOT NULL CHECK (DEPART_MINUTE BETWEEN 0 AND 1439)'),('SORT_ORDER','int','NOT NULL'),('VEHICLE_ID','id','NOT NULL REFERENCES VD_VEHICLE(ID)'),('DRIVER_ID','id','NOT NULL REFERENCES VD_DRIVER(ID)')], ['UNIQUE (ID, BRANCH_ID)','UNIQUE (BRANCH_ID, DEPART_MINUTE)'])
table('VD_REG_TRIP', [('SERVICE_DATE','date','NOT NULL'),('SHIFT_ID','id','NOT NULL REFERENCES VD_REG_SHIFT(ID)'),('VEHICLE_ID','id','NOT NULL REFERENCES VD_VEHICLE(ID)'),('DRIVER_ID','id','NOT NULL REFERENCES VD_DRIVER(ID)')], ['PRIMARY KEY (SERVICE_DATE, SHIFT_ID)'])
table('VD_REG_APP', [('ID','int','PRIMARY KEY'),('APP_NO','id','NOT NULL UNIQUE'),('SUBMIT_SEQ','int','NOT NULL UNIQUE'),('APPLICANT','text','NOT NULL'),('BRANCH_ID','id','NOT NULL REFERENCES VD_SITE(ID)'),('PICK_STATION_ID','id','NOT NULL'),('DROP_STATION_ID','id','NOT NULL'),('PICK_BUILDING','text',''),('DROP_BUILDING','text',''),('RECV_MODE','short','NOT NULL'),('SERVICE_DATE','date','NOT NULL'),('EXPECTED_MINUTE','int',''),('LOAD_MIN','int','NOT NULL CHECK (LOAD_MIN >= 0)'),('UNLOAD_MIN','int','NOT NULL CHECK (UNLOAD_MIN >= 0)'),('RECIPIENT_UNIT','text',''),('RECIPIENT_NAME','text',''),('RECIPIENT_PHONE','text',''),('AGENT_NAME','text',''),('AGENT_PHONE','text',''),('STATUS','short','NOT NULL'),('SHIFT_ID','id',''),('ARRIVAL_MINUTE','int',''),('EXPECT_DIFF_MIN','int',''),('NOTE','long',''),('FAILURE_REASON','short',''),('INCIDENT','text',''),('CREATED_AT','timestamp','NOT NULL'),('DELIVERED_AT','timestamp',''),('DELIVERED_BY','text',''),('MATCH_ATTEMPT','int','NOT NULL')], ["CHECK (RECV_MODE IN ('asap','exact'))", "CHECK (STATUS IN ('submitted','matched','unscheduled','delivered'))", 'CHECK (EXPECTED_MINUTE IS NULL OR EXPECTED_MINUTE BETWEEN 0 AND 1439)', 'FOREIGN KEY (PICK_STATION_ID, BRANCH_ID) REFERENCES VD_STATION(ID, BRANCH_ID)', 'FOREIGN KEY (DROP_STATION_ID, BRANCH_ID) REFERENCES VD_STATION(ID, BRANCH_ID)', 'FOREIGN KEY (SHIFT_ID, BRANCH_ID) REFERENCES VD_REG_SHIFT(ID, BRANCH_ID)', 'FOREIGN KEY (SERVICE_DATE, SHIFT_ID) REFERENCES VD_REG_TRIP(SERVICE_DATE, SHIFT_ID)', "CHECK ((STATUS IN ('matched','delivered') AND SHIFT_ID IS NOT NULL) OR (STATUS IN ('submitted','unscheduled') AND SHIFT_ID IS NULL))"])
# Unknown category codes deliberately remain legal: G03 uses the fallback factor.
table('VD_REG_ITEM', [('APPLICATION_ID','int','NOT NULL REFERENCES VD_REG_APP(ID)'),('LINE_NO','int','NOT NULL'),('NAME','text','NOT NULL'),('LENGTH_CM','real','NOT NULL CHECK (LENGTH_CM > 0)'),('WIDTH_CM','real','NOT NULL CHECK (WIDTH_CM > 0)'),('HEIGHT_CM','real','NOT NULL CHECK (HEIGHT_CM > 0)'),('CATEGORY','id','NOT NULL'),('QTY','int','NOT NULL CHECK (QTY > 0)'),('WEIGHT','real','NOT NULL CHECK (WEIGHT >= 0)')], ['PRIMARY KEY (APPLICATION_ID, LINE_NO)'])
table('VD_REG_MATCH_STEP', [('APPLICATION_ID','int','NOT NULL REFERENCES VD_REG_APP(ID)'),('ATTEMPT','int','NOT NULL'),('STEP_NO','int','NOT NULL'),('CODE','id','NOT NULL'),('MESSAGE','long','NOT NULL')], ['PRIMARY KEY (APPLICATION_ID, ATTEMPT, STEP_NO)'])
table('VD_MAINTENANCE', [('ID','int','PRIMARY KEY'),('VEHICLE_ID','id','NOT NULL REFERENCES VD_VEHICLE(ID)'),('FROM_DATE','date','NOT NULL'),('TO_DATE','date','NOT NULL'),('REASON','text','')], ['CHECK (FROM_DATE <= TO_DATE)'])
table('VD_DRIVER_LEAVE', [('ID','int','PRIMARY KEY'),('DRIVER_ID','id','NOT NULL REFERENCES VD_DRIVER(ID)'),('LEAVE_DATE','date','NOT NULL'),('FROM_MINUTE','int','NOT NULL'),('TO_MINUTE','int','NOT NULL'),('LEAVE_TYPE','text','')], ['CHECK (FROM_MINUTE < TO_MINUTE)'])
table('VD_REST_HOUSE', [('ID','id','PRIMARY KEY'),('NAME','text','NOT NULL')])
table('VD_TRAVEL_TIME', [('FROM_ID','id','NOT NULL'),('TO_ID','id','NOT NULL'),('SIZE_CLASS','short','NOT NULL'),('MINUTES','int','NOT NULL CHECK (MINUTES >= 0)')], ['PRIMARY KEY (FROM_ID, TO_ID, SIZE_CLASS)'])
table('VD_DRIVER_BREAK', [('AFTER_DRIVE_MIN','int','PRIMARY KEY'),('KIND','text','NOT NULL'),('COST_MIN','int','NOT NULL')])
table('VD_MIN_TRIP_DAYS', [('SIZE_CLASS','short','NOT NULL'),('SITE_ID','id','NOT NULL REFERENCES VD_SITE(ID)'),('DAYS','int','NOT NULL')], ['PRIMARY KEY (SIZE_CLASS, SITE_ID)'])
table('VD_BIZ_PLACE', [('NAME','text','PRIMARY KEY'),('SITE_ID','id','REFERENCES VD_SITE(ID)'),('IS_TRANSFER','int','NOT NULL')])
table('VD_BIZ_TRAVEL', [('ORIGIN','text','NOT NULL REFERENCES VD_BIZ_PLACE(NAME)'),('DESTINATION','text','NOT NULL REFERENCES VD_BIZ_PLACE(NAME)'),('MINUTES','int','NOT NULL')], ['PRIMARY KEY (ORIGIN, DESTINATION)'])
rows = {t: [] for t in tables}
def add(t, **values): rows[t].append(values)
def minutes(s): return int(s[:2])*60+int(s[3:])
add('VD_SCHEMA_VERSION', VERSION_NO=1)
add('VD_COUNTER', KEY_NAME='REG_APP', NEXT_VAL=1)
for s in db['sites']:
    add('VD_SITE', ID=s['id'], NAME=s['name'], SORT_ORDER=s['order'], LATE_RESTRICTED=int(s.get('lateRestricted',False)), RETURN_LOAD_BY=s.get('returnLoadBy'))
    for i,b in enumerate(s['buildings']): add('VD_BUILDING', ID=f"{s['id']}-B{i}", SITE_ID=s['id'], STATION_ID=None, NAME=b)
for s in db['stations']:
    add('VD_STATION', ID=s['id'], BRANCH_ID=s['branch'], NAME=s['name'], SORT_ORDER=s['order'])
    for b in s['buildings']: add('VD_BUILDING', ID=s['id']+'-'+b, SITE_ID=s['branch'], STATION_ID=s['id'], NAME=b)
for i,v in enumerate(db['vehicles']):
    d=v.get('dims',{})
    add('VD_VEHICLE',ID=v['id'],NAME=v['name'],POOL=v['pool'],SIZE_CLASS=v.get('sizeClass'),HOME_SITE=v['homeSite'],CURRENT_SITE=v['currentSite'],LENGTH_CM=d.get('l',0),WIDTH_CM=d.get('w',0),HEIGHT_CM=d.get('h',0),VOLUME=v.get('volume',0),WEIGHT_LIMIT=v.get('weight',0),SEATS=v.get('seats',0),SORT_ORDER=i)
for i,d in enumerate(db['drivers']): add('VD_DRIVER',ID=d['id'],NAME=d['name'],POOL=d['pool'],HOME_SITE=d['homeSite'],CURRENT_SITE=d['currentSite'],SORT_ORDER=i)
for i,c in enumerate(db['wasteFactors']): add('VD_CARGO_CATEGORY',CODE=c['code'],NAME=c['name'],FACTOR=c['factor'],ACTIVE=int(c['active']),SORT_ORDER=i)
for key,val in {'WasteDefault':db['wasteDefault'],'InterStationMin':3,'ShiftHandleBudget':60,'DailyDutyMin':db['dailyDutyMin'],'PrepMin':db['prepMin'],'CloseMin':db['closeMin'],'MaxTripDays':db['maxTripDays'],'BizBuffer':db['bizBuffer'],'DirectLockWindowMin':db['directLockWindowMin']}.items(): add('VD_CONFIG',KEY_NAME=key,NUM_VALUE=val)
for i,s in enumerate(db['regionalShifts']): add('VD_REG_SHIFT',ID=s['id'],BRANCH_ID=s['branch'],LABEL=s['label'],DEPART_MINUTE=minutes(s['depart']),SORT_ORDER=i,VEHICLE_ID=s['vehicle'],DRIVER_ID='DR1' if s['vehicle']=='V-L01' else 'DR2')
for i,m in enumerate(db['maintenance']): add('VD_MAINTENANCE',ID=i+1,VEHICLE_ID=m['vehicle'],FROM_DATE=m['from'],TO_DATE=m['to'],REASON=m['reason'])
for i,m in enumerate(db['driverLeaves']): add('VD_DRIVER_LEAVE',ID=i+1,DRIVER_ID=m['driver'],LEAVE_DATE=m['date'],FROM_MINUTE=minutes(m['from']),TO_MINUTE=minutes(m['to']),LEAVE_TYPE=m['type'])
for r in db['restHouses']: add('VD_REST_HOUSE',ID=r['id'],NAME=r['name'])
for cls,values in db['siteTravel'].items():
    for key,val in values.items():
        a,b=key.split('|');add('VD_TRAVEL_TIME',FROM_ID=a,TO_ID=b,SIZE_CLASS=cls,MINUTES=val)
for b in db['driverBreaks']: add('VD_DRIVER_BREAK',AFTER_DRIVE_MIN=b['afterDriveMin'],KIND=b['kind'],COST_MIN=b['costMin'])
for cls,values in db['minTripDays'].items():
    for site,val in values.items(): add('VD_MIN_TRIP_DAYS',SIZE_CLASS=cls,SITE_ID=site,DAYS=val)
for place in dict.fromkeys(db['bizOrigins']+db['bizDests']): add('VD_BIZ_PLACE',NAME=place,SITE_ID=db['bizOriginSite'].get(place),IS_TRANSFER=int(place in db['transferPoints']))
for key,val in db['bizTravel'].items():
    a,b=key.split('|');add('VD_BIZ_TRAVEL',ORIGIN=a,DESTINATION=b,MINUTES=val)
for dialect in ('sqlite','oracle'):
    types = {'id':'TEXT','short':'TEXT','text':'TEXT','long':'TEXT','int':'INTEGER','real':'NUMERIC','date':'TEXT','timestamp':'TEXT'} if dialect=='sqlite' else {'id':'VARCHAR2(30 CHAR)','short':'VARCHAR2(30 CHAR)','text':'VARCHAR2(200 CHAR)','long':'VARCHAR2(2000 CHAR)','int':'NUMBER(19,0)','real':'NUMBER(24,9)','date':'DATE','timestamp':'TIMESTAMP'}
    schema=['-- Generated by tools/generate-database.py; fictional data only.']
    if dialect=='sqlite': schema.append('PRAGMA foreign_keys = ON;')
    for t,(cols,cons) in tables.items():
        defs=[f'    {name} {types[kind]} {rule}'.rstrip() for name,kind,rule in cols]
        defs+=['    '+c for c in cons]
        schema.append('CREATE TABLE '+t+' (\n'+',\n'.join(defs)+'\n);')
    schema += ['CREATE INDEX IX_REG_APP_SHIFT ON VD_REG_APP (SERVICE_DATE, SHIFT_ID, STATUS);','CREATE INDEX IX_REG_APP_BRANCH ON VD_REG_APP (BRANCH_ID, STATUS, SUBMIT_SEQ);','CREATE INDEX IX_REG_APP_DROP ON VD_REG_APP (DROP_STATION_ID);']
    if dialect=='oracle': schema.append('CREATE SEQUENCE VD_REG_APP_SEQ START WITH 1 INCREMENT BY 1 NOCYCLE;')
    def literal(value,kind):
        if value is None or value=='': return 'NULL'
        if isinstance(value,(int,float)): return str(value)
        q="'"+str(value).replace("'","''")+"'"
        return 'DATE '+q if dialect=='oracle' and kind=='date' else q
    seed=['-- Generated fictional master data. Run once after 001_schema.sql.']
    for t,(cols,_) in tables.items():
        for row in rows[t]:
            names=list(row); kinds={a:b for a,b,_ in cols}
            seed.append('INSERT INTO '+t+' ('+', '.join(names)+') VALUES ('+', '.join(literal(row[n],kinds[n]) for n in names)+');')
    if dialect=='oracle': seed.append('COMMIT;')
    out=ROOT/'database'/dialect
    (out/'001_schema.sql').write_text('\n\n'.join(schema)+'\n')
    (out/'002_seed.sql').write_text('\n'.join(seed)+'\n')
    verify=['-- Counts must match database/seed-counts.json.']+[f'SELECT \'{t}\' AS TABLE_NAME, COUNT(*) AS ROW_COUNT FROM {t};' for t in tables]
    (out/'003_verify.sql').write_text('\n'.join(verify)+'\n')
(ROOT/'database/seed-counts.json').write_text(json.dumps({t:len(r) for t,r in rows.items()},indent=2)+'\n')
# Include runtime SQLite initialization scripts in Web publish output.
for name in ('001_schema.sql','002_seed.sql'):
    (ROOT/'src/VD.Web/App_Data'/name).write_text((ROOT/'database/sqlite'/name).read_text())
# A concise dictionary is generated from the same schema, so it cannot omit columns.
dictionary=['# 資料字典','', 'SQLite / Oracle 19c 兩套 SQL 由同一份邏輯結構產生。schema version = 1。', '', '類別代碼不加外鍵：保留 G03 未知類別使用保底係數的規則。建物自選／其他文字保存於申請快照。', '']
for t,(cols,cons) in tables.items():
    dictionary += ['## '+t,'','| 欄位 | 邏輯型別 | 約束 |','|---|---|---|']
    dictionary += [f'| {n} | {k} | {r} |' for n,k,r in cols]
    dictionary += ['',*['- '+c for c in cons],'']
(ROOT/'database/DATA-DICTIONARY.md').write_text('\n'.join(dictionary))
print(f'Generated {len(tables)} tables, {sum(map(len,rows.values()))} seed rows for SQLite and Oracle.')
