using EVWebApi.DTOs.Pagination;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Serialization;

namespace EVWebApi.DTOs.Document
{
    public enum SearchType
    {
        starts_with,
        anywhere 
    }
    public enum AmountType
    {
        equal,
        greater,
        less,
        between

    }
    public enum DateType
    {
        before,
        after,
        on,
        between
    }

    public class DocumentQueryParameters 
    {
        //public int Offset { get; set; } = 0;   // number of records to skip
        //public int Limit { get; set; } = 100;
        [FromQuery(Name = "page_number")]
        public int PageNumber { get; set; } = 1;
        [FromQuery(Name = "page_size")]
        public int PageSize { get; set; } = 10;

        [FromQuery(Name = "vendor")]
        public string? VendorNumber { get; set; }
        [FromQuery(Name = "gst")]
        public decimal? GST { get; set; }
        [FromQuery(Name = "invoice")]
        public string? InvoiceNumber { get; set; }
        [FromQuery(Name = "po")]
        public string? PoNumber { get; set; }

        [FromQuery(Name = "document_type")]
        public string? DocType { get; set; }

        [FromQuery(Name = "employee_id")]
        public string? EmployeeId { get; set; }
        [FromQuery(Name = "name")]//------for all names vendor name, name in hr
        public string? Name { get; set; }
        [FromQuery(Name = "contact_number")]
        public string? ContactNumber { get; set; }
        [FromQuery(Name = "designation")]
        public string? Designation { get; set; }
        [FromQuery(Name = "check_number")]
        public string? CheckNumber { get; set; }
        [FromQuery(Name = "status")]
        public string? Status { get; set; }


        [FromQuery(Name = "search_type")]
        public SearchType? SearchType { get; set; }
        [FromQuery(Name = "amount_type")]
        public AmountType? AmountType { get; set; }
        [FromQuery(Name = "date_type")]
        public DateType? DateType { get; set; }

        [FromQuery(Name = "amount_from")]//-------------for all amount check amount, inv amount
        public decimal? Amount { get; set; }
        [FromQuery(Name = "amount_to")]
        public decimal? AmountTo { get; set; }


        [FromQuery(Name = "statement_from_date")]
        public DateTime? StatementDate { get; set; }
        [FromQuery(Name = "statement_to_date")]
        public DateTime? StatementDateTo { get; set; }


        [FromQuery(Name = "paidamount_from")]
        public decimal? PaidAmount { get; set; }
        [FromQuery(Name = "paidamount_to")]
        public decimal? PaidAmountTo { get; set; }

        [FromQuery(Name = "doj_from_date")]
        public DateTime? DOJ { get; set; }
        [FromQuery(Name = "doj_to_date")]
        public DateTime? DOJDateTo { get; set; }


        [FromQuery(Name = "dob_from_date")]
        public DateTime? DOB { get; set; }
        [FromQuery(Name = "dob_to_date")]
        public DateTime? DOBDateTo { get; set; }


        [FromQuery(Name = "from_date")]//----------for both inv date and check date
        public DateTime? InvoiceDate { get; set; }
        [FromQuery(Name = "to_date")]
        public DateTime? InvoiceDateTo { get; set; }
    }
}
