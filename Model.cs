using System;
using System.Collections.Generic;
using System.Text;

namespace EDIConverter 
{
	public class Model
	{
        public  string ReferenceNumber { get; set; }
        public  string IssueDate { get; set; }
        public  string DocumentType { get; set; }
        public  Supplier Supplier { get; set; }

        public List<String> OrderNumbers { get; set; }
            
    }

    public class Address
    {
        public string PostalCode { get; set; }
        public string AddressLine { get; set; }

		}

	public class Supplier
	{
        public string Name { get; set; }
        public string Vat { get; set; }
        public string SupplierCode { get; set; }

        public Address Address { get; set; }
		public List<Address> Addresses { get; set; }
	}
}
