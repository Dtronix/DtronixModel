﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DtxModeler.Ddl {
	public partial class Table {

	}



	public partial class Association {

		[XmlIgnore]
		public Association ChildAssociation;

		[XmlIgnore]
		public Association ParentAssociation;

		[XmlIgnore]
		public Column OtherKeyColumn;

		[XmlIgnore]
		public Column ThisKeyColumn;

		[XmlIgnore]
		public Table Table;
	}

	public partial class Column {

		/// <remarks/>
		[XmlIgnore]
		public Table Table;
	}

	public partial class Index {

		/// <remarks/>
		[XmlIgnore]
		public Table Table;
	}
}
