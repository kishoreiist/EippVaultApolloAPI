using EVWebApi.DTOs.Pagination;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Serialization;

namespace EVWebApi.DTOs.Document
{
    public enum SearchType
    {
        [EnumMember(Value = "starts_with")]
        StartsWith,
        [EnumMember(Value = "anywhere")]
        AnyWhere,
        [EnumMember(Value = "equal")]
        Equals,
        [EnumMember(Value = "greater")]
        Greater,
        [EnumMember(Value = "less")]
        Less,
        [EnumMember(Value = "between")]
        Between,
        [EnumMember(Value = "before")]
        Before,
        [EnumMember(Value = "after")]
        After,
        [EnumMember(Value = "on")]
        On
    }

    public class DocumentQueryParameters 
    {
        public int Offset { get; set; } = 0;   // number of records to skip
        public int Limit { get; set; } = 100;

        [FromQuery(Name ="vendor_number")]
        public string? VendorNumber { get; set; }
        [FromQuery(Name = "gst")]
        public decimal? GST { get; set; }
        [FromQuery(Name = "invoice_number")]
        public string? InvoiceNumber { get; set; }
        [FromQuery(Name = "po_number")]
        public string? PoNumber { get; set; }
        [FromQuery(Name = "date")]//----------for both inv date and check date
        public DateTime? InvoiceDate { get; set; }
        [FromQuery(Name = "amount")]//-------------for all amount check amount, inv amount
        public decimal? Amount { get; set; }
        [FromQuery(Name = "statement_date")]
        public DateTime? StatementDate { get; set; }
        [FromQuery(Name = "employee_id")]
        public string? EmployeeId { get; set; }
        [FromQuery(Name = "name")]//------for all names vendor name, name in hr
        public string? Name { get; set; }
        [FromQuery(Name = "contact_number")]
        public string? ContactNumber { get; set; }
        [FromQuery(Name = "designation")]
        public string? Designation { get; set; }
        [FromQuery(Name = "doj")]
        public DateTime? DOJ { get; set; }
        [FromQuery(Name = "check_number")]
        public string? CheckNumber { get; set; }
        [FromQuery(Name = "paid_amount")]
        public decimal? PaidAmount { get; set; }


        [FromQuery(Name = "search_type")]
        public SearchType? SearchType { get; set; }
        [FromQuery(Name = "amount_from")]
        public string? AmountFrom { get; set; }
        [FromQuery(Name = "amount_to")]
        public string? AmountTo { get; set; }

        [FromQuery(Name = "paidamount_from")]
        public string? PaidAmountFrom { get; set; }
        [FromQuery(Name = "paidamount_to")]
        public string? PaidAmountTo { get; set; }

        [FromQuery(Name = "doj_from_date")]
        public string? DOJDateFrom { get; set; }
        [FromQuery(Name = "doj_to_date")]
        public string? DOJDateTo { get; set; }

        [FromQuery(Name = "from_date")]
        public string? DateFrom { get; set; }
        [FromQuery(Name = "to_date")]
        public string? DateTo { get; set; }
    }
}
