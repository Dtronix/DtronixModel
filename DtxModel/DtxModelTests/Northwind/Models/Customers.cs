﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using DtxModel;
using System.Data.SqlClient;
using System.Data.Common;
using System.Reflection;

namespace DtxModelTests.Northwind.Models {
	class Customers : Model {

		private bool _CustomerIDChanged = false;

		private string _CustomerID;

		public string CustomerID {
			get { return _CustomerID; }
			set {
				_CustomerID = value;
				_CustomerIDChanged = true;
			}
		}

		private bool _CompanyNameChanged = false;

		private string _CompanyName;

		public string CompanyName {
			get { return _CompanyName; }
			set {
				_CompanyName = value;
				_CompanyNameChanged = true;
			}
		}

		private bool _ContactNameChanged = false;

		private string _ContactName;

		public string ContactName {
			get { return _ContactName; }
			set {
				_ContactName = value;
				_ContactNameChanged = true;
			}
		}

		private bool _AddressChanged = false;
		private string _Address;

		public string Address {
			get { return _Address; }
			set {
				_Address = value;
				_AddressChanged = true;
			}
		}

		private bool _CityChanged = false;
		private string _City;

		public string City {
			get { return _City; }
			set {
				_City = value;
				_CityChanged = true;
			}
		}

		private bool _RegionChanged = false;
		private string _Region;

		public string Region {
			get { return _Region; }
			set {
				_Region = value;
				_RegionChanged = true;
			}
		}

		private bool _PostalCodeChanged = false;
		private string _PostalCode;

		public string PostalCode {
			get { return _PostalCode; }
			set {
				_PostalCode = value;
				_PostalCodeChanged = true;
			}
		}

		private bool _CountryChanged = false;
		private string _Country;

		public string Country {
			get { return _Country; }
			set {
				_Country = value;
				_CountryChanged = true;
			}
		}

		private bool _PhoneChanged = false;
		private string _Phone;

		public string Phone {
			get { return _Phone; }
			set {
				_Phone = value;
				_PhoneChanged = true;
			}
		}

		private bool _FaxChanged = false;
		private string _Fax;

		public string Fax {
			get { return _Fax; }
			set {
				_Fax = value;
				_FaxChanged = true;
			}
		}

		public Customers() : this(null, null) { }

		public Customers(DbDataReader reader, DbConnection connection) : base(connection) {
			if (reader == null) {
				return;
			}

			int length = reader.FieldCount;
			for (int i = 0; i < length; i++) {
				switch (reader.GetName(i)) {
					case "rowid": _rowid = (long)reader.GetValue(i); break;
					case "CustomerID": _CustomerID = reader.GetValue(i) as string; break;
					case "CompanyName": _CompanyName = reader.GetValue(i) as string; break;
					case "ContactName": _ContactName = reader.GetValue(i) as string; break;
					case "Address": _Address = reader.GetValue(i) as string; break;
					case "City": _City = reader.GetValue(i) as string; break;
					case "Region": _Region = reader.GetValue(i) as string; break;
					case "PostalCode": _PostalCode = reader.GetValue(i) as string; break;
					case "Country": _Country = reader.GetValue(i) as string; break;
					case "Phone": _Phone = reader.GetValue(i) as string; break;
					case "Fax": _Fax = reader.GetValue(i) as string; break;
					default:
						break;
				}
			}
		}

		public override Dictionary<string, object> getChangedValues() {
			var changed = new Dictionary<string, object>();
			if (_CustomerIDChanged)
				changed.Add("CustomerID", _CustomerID);
			if (_CompanyNameChanged)
				changed.Add("CompanyName", _CompanyName);
			if (_ContactNameChanged)
				changed.Add("ContactName", _ContactName);
			if (_AddressChanged)
				changed.Add("Address", _Address);
			if (_CityChanged)
				changed.Add("City", _City);
			if (_RegionChanged)
				changed.Add("Region", _Region);
			if (_PostalCodeChanged)
				changed.Add("PostalCode", _PostalCode);
			if (_CountryChanged)
				changed.Add("Country", _Country);
			if (_PhoneChanged)
				changed.Add("Phone", _Phone);
			if (_FaxChanged)
				changed.Add("Fax", _Fax);

			return changed;
		}

		public override Dictionary<string, object> getAllValues() {
			var columns = new Dictionary<string, object>();
			columns.Add("CustomerID", _CustomerID);
			columns.Add("CompanyName", _CompanyName);
			columns.Add("ContactName", _ContactName);
			columns.Add("Address", _Address);
			columns.Add("City", _City);
			columns.Add("Region", _Region);
			columns.Add("PostalCode", _PostalCode);
			columns.Add("Country", _Country);
			columns.Add("Phone", _Phone);
			columns.Add("Fax", _Fax);

			return columns;
		}

		public override string[] getColumns() {
			return new string[] {
				"CustomerID",
				"CompanyName",
				"ContactName",
				"Address",
				"City",
				"Region",
				"PostalCode",
				"Country",
				"Phone",
				"Fax",
			};
		}

	}
}
