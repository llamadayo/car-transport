# 資料字典

SQLite / Oracle 19c 兩套 SQL 由同一份邏輯結構產生。schema version = 1。

類別代碼不加外鍵：保留 G03 未知類別使用保底係數的規則。建物自選／其他文字保存於申請快照。

## VD_SCHEMA_VERSION

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| VERSION_NO | int | PRIMARY KEY |


## VD_COUNTER

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| KEY_NAME | id | PRIMARY KEY |
| NEXT_VAL | int | NOT NULL |


## VD_SITE

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ID | id | PRIMARY KEY |
| NAME | text | NOT NULL |
| SORT_ORDER | int | NOT NULL UNIQUE |
| LATE_RESTRICTED | int | NOT NULL |
| RETURN_LOAD_BY | short |  |


## VD_STATION

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ID | id | PRIMARY KEY |
| BRANCH_ID | id | NOT NULL REFERENCES VD_SITE(ID) |
| NAME | text | NOT NULL |
| SORT_ORDER | int | NOT NULL |

- UNIQUE (ID, BRANCH_ID)
- UNIQUE (BRANCH_ID, SORT_ORDER)

## VD_BUILDING

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ID | id | PRIMARY KEY |
| SITE_ID | id | NOT NULL REFERENCES VD_SITE(ID) |
| STATION_ID | id |  |
| NAME | text | NOT NULL |

- FOREIGN KEY (STATION_ID, SITE_ID) REFERENCES VD_STATION(ID, BRANCH_ID)

## VD_VEHICLE

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ID | id | PRIMARY KEY |
| NAME | text | NOT NULL |
| POOL | short | NOT NULL |
| SIZE_CLASS | short |  |
| HOME_SITE | id | REFERENCES VD_SITE(ID) |
| CURRENT_SITE | id | REFERENCES VD_SITE(ID) |
| LENGTH_CM | real | NOT NULL |
| WIDTH_CM | real | NOT NULL |
| HEIGHT_CM | real | NOT NULL |
| VOLUME | real | NOT NULL |
| WEIGHT_LIMIT | real | NOT NULL |
| SEATS | int | NOT NULL |
| SORT_ORDER | int | NOT NULL |

- CHECK (POOL IN ('LOGI','BIZ'))

## VD_DRIVER

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ID | id | PRIMARY KEY |
| NAME | text | NOT NULL |
| POOL | short | NOT NULL |
| HOME_SITE | id | REFERENCES VD_SITE(ID) |
| CURRENT_SITE | id | REFERENCES VD_SITE(ID) |
| SORT_ORDER | int | NOT NULL |


## VD_CARGO_CATEGORY

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| CODE | id | PRIMARY KEY |
| NAME | text | NOT NULL |
| FACTOR | real | NOT NULL CHECK (FACTOR > 0) |
| ACTIVE | int | NOT NULL CHECK (ACTIVE IN (0,1)) |
| SORT_ORDER | int | NOT NULL |


## VD_CONFIG

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| KEY_NAME | id | PRIMARY KEY |
| NUM_VALUE | real | NOT NULL |


## VD_REG_SHIFT

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ID | id | PRIMARY KEY |
| BRANCH_ID | id | NOT NULL REFERENCES VD_SITE(ID) |
| LABEL | text | NOT NULL |
| DEPART_MINUTE | int | NOT NULL CHECK (DEPART_MINUTE BETWEEN 0 AND 1439) |
| SORT_ORDER | int | NOT NULL |
| VEHICLE_ID | id | NOT NULL REFERENCES VD_VEHICLE(ID) |
| DRIVER_ID | id | NOT NULL REFERENCES VD_DRIVER(ID) |

- UNIQUE (ID, BRANCH_ID)
- UNIQUE (BRANCH_ID, DEPART_MINUTE)

## VD_REG_TRIP

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| SERVICE_DATE | date | NOT NULL |
| SHIFT_ID | id | NOT NULL REFERENCES VD_REG_SHIFT(ID) |
| VEHICLE_ID | id | NOT NULL REFERENCES VD_VEHICLE(ID) |
| DRIVER_ID | id | NOT NULL REFERENCES VD_DRIVER(ID) |

- PRIMARY KEY (SERVICE_DATE, SHIFT_ID)

## VD_REG_APP

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ID | int | PRIMARY KEY |
| APP_NO | id | NOT NULL UNIQUE |
| SUBMIT_SEQ | int | NOT NULL UNIQUE |
| APPLICANT | text | NOT NULL |
| BRANCH_ID | id | NOT NULL REFERENCES VD_SITE(ID) |
| PICK_STATION_ID | id | NOT NULL |
| DROP_STATION_ID | id | NOT NULL |
| PICK_BUILDING | text |  |
| DROP_BUILDING | text |  |
| RECV_MODE | short | NOT NULL |
| SERVICE_DATE | date | NOT NULL |
| EXPECTED_MINUTE | int |  |
| LOAD_MIN | int | NOT NULL CHECK (LOAD_MIN >= 0) |
| UNLOAD_MIN | int | NOT NULL CHECK (UNLOAD_MIN >= 0) |
| RECIPIENT_UNIT | text |  |
| RECIPIENT_NAME | text |  |
| RECIPIENT_PHONE | text |  |
| AGENT_NAME | text |  |
| AGENT_PHONE | text |  |
| STATUS | short | NOT NULL |
| SHIFT_ID | id |  |
| ARRIVAL_MINUTE | int |  |
| EXPECT_DIFF_MIN | int |  |
| NOTE | long |  |
| FAILURE_REASON | short |  |
| INCIDENT | text |  |
| CREATED_AT | timestamp | NOT NULL |
| DELIVERED_AT | timestamp |  |
| DELIVERED_BY | text |  |
| MATCH_ATTEMPT | int | NOT NULL |

- CHECK (RECV_MODE IN ('asap','exact'))
- CHECK (STATUS IN ('submitted','matched','unscheduled','delivered'))
- CHECK (EXPECTED_MINUTE IS NULL OR EXPECTED_MINUTE BETWEEN 0 AND 1439)
- FOREIGN KEY (PICK_STATION_ID, BRANCH_ID) REFERENCES VD_STATION(ID, BRANCH_ID)
- FOREIGN KEY (DROP_STATION_ID, BRANCH_ID) REFERENCES VD_STATION(ID, BRANCH_ID)
- FOREIGN KEY (SHIFT_ID, BRANCH_ID) REFERENCES VD_REG_SHIFT(ID, BRANCH_ID)
- FOREIGN KEY (SERVICE_DATE, SHIFT_ID) REFERENCES VD_REG_TRIP(SERVICE_DATE, SHIFT_ID)
- CHECK ((STATUS IN ('matched','delivered') AND SHIFT_ID IS NOT NULL) OR (STATUS IN ('submitted','unscheduled') AND SHIFT_ID IS NULL))

## VD_REG_ITEM

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| APPLICATION_ID | int | NOT NULL REFERENCES VD_REG_APP(ID) |
| LINE_NO | int | NOT NULL |
| NAME | text | NOT NULL |
| LENGTH_CM | real | NOT NULL CHECK (LENGTH_CM > 0) |
| WIDTH_CM | real | NOT NULL CHECK (WIDTH_CM > 0) |
| HEIGHT_CM | real | NOT NULL CHECK (HEIGHT_CM > 0) |
| CATEGORY | id | NOT NULL |
| QTY | int | NOT NULL CHECK (QTY > 0) |
| WEIGHT | real | NOT NULL CHECK (WEIGHT >= 0) |

- PRIMARY KEY (APPLICATION_ID, LINE_NO)

## VD_REG_MATCH_STEP

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| APPLICATION_ID | int | NOT NULL REFERENCES VD_REG_APP(ID) |
| ATTEMPT | int | NOT NULL |
| STEP_NO | int | NOT NULL |
| CODE | id | NOT NULL |
| MESSAGE | long | NOT NULL |

- PRIMARY KEY (APPLICATION_ID, ATTEMPT, STEP_NO)

## VD_MAINTENANCE

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ID | int | PRIMARY KEY |
| VEHICLE_ID | id | NOT NULL REFERENCES VD_VEHICLE(ID) |
| FROM_DATE | date | NOT NULL |
| TO_DATE | date | NOT NULL |
| REASON | text |  |

- CHECK (FROM_DATE <= TO_DATE)

## VD_DRIVER_LEAVE

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ID | int | PRIMARY KEY |
| DRIVER_ID | id | NOT NULL REFERENCES VD_DRIVER(ID) |
| LEAVE_DATE | date | NOT NULL |
| FROM_MINUTE | int | NOT NULL |
| TO_MINUTE | int | NOT NULL |
| LEAVE_TYPE | text |  |

- CHECK (FROM_MINUTE < TO_MINUTE)

## VD_REST_HOUSE

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ID | id | PRIMARY KEY |
| NAME | text | NOT NULL |


## VD_TRAVEL_TIME

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| FROM_ID | id | NOT NULL |
| TO_ID | id | NOT NULL |
| SIZE_CLASS | short | NOT NULL |
| MINUTES | int | NOT NULL CHECK (MINUTES >= 0) |

- PRIMARY KEY (FROM_ID, TO_ID, SIZE_CLASS)

## VD_DRIVER_BREAK

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| AFTER_DRIVE_MIN | int | PRIMARY KEY |
| KIND | text | NOT NULL |
| COST_MIN | int | NOT NULL |


## VD_MIN_TRIP_DAYS

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| SIZE_CLASS | short | NOT NULL |
| SITE_ID | id | NOT NULL REFERENCES VD_SITE(ID) |
| DAYS | int | NOT NULL |

- PRIMARY KEY (SIZE_CLASS, SITE_ID)

## VD_BIZ_PLACE

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| NAME | text | PRIMARY KEY |
| SITE_ID | id | REFERENCES VD_SITE(ID) |
| IS_TRANSFER | int | NOT NULL |


## VD_BIZ_TRAVEL

| 欄位 | 邏輯型別 | 約束 |
|---|---|---|
| ORIGIN | text | NOT NULL REFERENCES VD_BIZ_PLACE(NAME) |
| DESTINATION | text | NOT NULL REFERENCES VD_BIZ_PLACE(NAME) |
| MINUTES | int | NOT NULL |

- PRIMARY KEY (ORIGIN, DESTINATION)
