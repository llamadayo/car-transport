"""Portable checks execute real SQLite DDL/seed; they do not claim C#/Oracle runtime coverage."""
import json, sqlite3, unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
class DatabaseTests(unittest.TestCase):
 def setUp(self):
  self.db=sqlite3.connect(':memory:')
  for name in ('001_schema.sql','002_seed.sql'):self.db.executescript((ROOT/'database/sqlite'/name).read_text())
 def tearDown(self):self.db.close()
 def test_all_seed_counts(self):
  for table,count in json.loads((ROOT/'database/seed-counts.json').read_text()).items():
   with self.subTest(table=table):self.assertEqual(self.db.execute('SELECT COUNT(*) FROM '+table).fetchone()[0],count)
 def test_integrity(self):
  self.assertEqual(self.db.execute('PRAGMA integrity_check').fetchone()[0],'ok')
  self.assertEqual(self.db.execute('PRAGMA foreign_key_check').fetchall(),[])
 def test_ten_branches_nine_stations_eleven_hourly_shifts(self):
  for (branch,) in self.db.execute('SELECT ID FROM VD_SITE'):
   self.assertEqual([x[0] for x in self.db.execute('SELECT SORT_ORDER FROM VD_STATION WHERE BRANCH_ID=? ORDER BY SORT_ORDER',(branch,))],list(range(1,10)))
   self.assertEqual([x[0] for x in self.db.execute('SELECT DEPART_MINUTE FROM VD_REG_SHIFT WHERE BRANCH_ID=? ORDER BY DEPART_MINUTE',(branch,))],list(range(480,1081,60)))
 def test_building_cannot_reference_another_branch(self):
  with self.assertRaises(sqlite3.IntegrityError):self.db.execute("INSERT INTO VD_BUILDING VALUES ('bad','D1','D2-100','invalid')")
 def app(self,**over):
  data=dict(ID=1,APP_NO='LA001',SUBMIT_SEQ=1,APPLICANT="測試 O'Brien",BRANCH_ID='D1',PICK_STATION_ID='D1-100',DROP_STATION_ID='D1-900',RECV_MODE='exact',SERVICE_DATE='2026-09-21',LOAD_MIN=10,UNLOAD_MIN=5,STATUS='unscheduled',CREATED_AT='2026-09-21T07:00:00',MATCH_ATTEMPT=1)
  data.update(over);self.db.execute('INSERT INTO VD_REG_APP ('+','.join(data)+') VALUES ('+','.join('?' for _ in data)+')',tuple(data.values()))
 def test_cross_branch_application_rejected(self):
  with self.assertRaises(sqlite3.IntegrityError):self.app(DROP_STATION_ID='D2-900')
 def test_matched_requires_shift_and_trip(self):
  with self.assertRaises(sqlite3.IntegrityError):self.app(STATUS='matched')
  with self.assertRaises(sqlite3.IntegrityError):self.app(STATUS='matched',SHIFT_ID='D1-R1')
 def test_trip_date_is_part_of_foreign_key(self):
  self.db.execute("INSERT INTO VD_REG_TRIP VALUES ('2026-09-22','D1-R1','V-L01','DR1')")
  with self.assertRaises(sqlite3.IntegrityError):self.app(STATUS='matched',SHIFT_ID='D1-R1')
 def test_unknown_category_legal_nonpositive_qty_illegal(self):
  self.app();values=(1,1,'測試箱',50,40,30,'UNKNOWN',1,10)
  self.db.execute('INSERT INTO VD_REG_ITEM VALUES (?,?,?,?,?,?,?,?,?)',values)
  with self.assertRaises(sqlite3.IntegrityError):self.db.execute('INSERT INTO VD_REG_ITEM VALUES (?,?,?,?,?,?,?,?,?)',(1,2,'測試箱',50,40,30,'BOX',0,10))
 def test_cargo_fk_and_transaction_rollback(self):
  with self.assertRaises(sqlite3.IntegrityError):self.db.execute("INSERT INTO VD_REG_ITEM VALUES (123,1,'orphan',1,1,1,'BOX',1,1)")
  self.db.rollback();self.app();self.db.rollback();self.assertEqual(self.db.execute('SELECT COUNT(*) FROM VD_REG_APP').fetchone()[0],0)
 def test_web_scripts_are_exact_sql_copies(self):
  for name in ('001_schema.sql','002_seed.sql'):self.assertEqual((ROOT/'database/sqlite'/name).read_bytes(),(ROOT/'src/VD.Web/App_Data'/name).read_bytes())
if __name__=='__main__':unittest.main()
