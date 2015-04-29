CREATE TABLE Users (
	rowid INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT ,
	username TEXT NOT NULL ,
	password TEXT NOT NULL ,
	last_logged INTEGER NOT NULL 
);



CREATE TABLE Logs (
	rowid INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT ,
	Users_rowid INTEGER NOT NULL ,
	text TEXT NOT NULL 
);



CREATE TABLE AllTypes (
	id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT ,
	db_int16 SMALLINT NOT NULL ,
	db_int32 INTEGER NOT NULL ,
	db_int64 INTEGER NOT NULL ,
	db_byte_array BLOB NOT NULL ,
	db_byte BLOB NOT NULL ,
	db_date_time TEXT NOT NULL ,
	db_decimal REAL NOT NULL ,
	db_float FLOAT NOT NULL ,
	db_double DOUBLE NOT NULL ,
	db_bool BOOLEAN NOT NULL ,
	db_string TEXT NOT NULL ,
	db_char CHAR NOT NULL 
);


