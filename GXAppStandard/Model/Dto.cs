using System.Collections.Generic;

namespace GXUploader.Dtos
{
    // Root payload
    public class UploadRoot
    {
        public List<UploadData> data { get; set; } = new();
    }

    public class UploadData
    {
        public string OriginApplication { get; set; } = "RProPrismWeb";
        public PrimaryItemDefinition PrimaryItemDefinition { get; set; } = new();
        public List<InventoryItem> InventoryItems { get; set; } = new();

        public bool UpdateStyleDefinition { get; set; }
        public bool UpdateStyleCost { get; set; }
        public bool UpdateStylePrice { get; set; }


        public bool UpdateStyleLty { get; set; }

        public string? DefaultReasonSidForQtyMemo { get; set; }
        public string? DefaultReasonSidForCostMemo { get; set; }
        public string? DefaultReasonSidForPriceMemo { get; set; }
    }

    public class PrimaryItemDefinition
    {
        public string? dcssid { get; set; }
        public string? vendsid { get; set; }
        public string? description1 { get; set; }
        public string? description2 { get; set; }
        public string? attribute { get; set; }
        public string? itemsize { get; set; }

        public string? alu { get; set; }
        public string? upc { get; set; }

        public string? sid { get; set; } = null;
    }

    public class InventoryItem
    {
        public string? sid { get; set; }
        public string? sbssid { get; set; }
        public string? dcssid { get; set; }
        public string? vendsid { get; set; }
        public string? taxcodesid { get; set; }

        public string? description1 { get; set; }
        public string? description2 { get; set; }
        public string? description3 { get; set; }
        public string? description4 { get; set; }

        public string? text1 { get; set; }
        public string? text2 { get; set; }
        public string? text3 { get; set; }
        public string? text4 { get; set; }
        public string? text5 { get; set; }
        public string? text6 { get; set; }
        public string? text7 { get; set; }
        public string? text8 { get; set; }
        public string? text9 { get; set; }
        public string? text10 { get; set; }

        public string? attribute { get; set; }
        public string? itemsize { get; set; }

        public string? udf1string { get; set; }
        public string? udf2string { get; set; }
        public string? udf3string { get; set; }
        public string? udf4string { get; set; }
        public string? udf5string { get; set; }
        public string? udf6string { get; set; }
        public string? udf7string { get; set; }
        public string? udf8string { get; set; }
        public string? udf9string { get; set; }
        public string? udf10string { get; set; }
        public string? udf11string { get; set; }
        public string? udf12string { get; set; }
        public string? udf13string { get; set; }
        public string? udf14string { get; set; }
        public string? udf15string { get; set; }
        public decimal cost { get; set; }
        public decimal fstprice { get; set; }
        public decimal lastrcvdcost { get; set; }
        public int spif { get; set; }
        public int useqtydecimals { get; set; }
        public bool regional { get; set; }
        public bool active { get; set; }
        public int noninventory { get; set; }
        public string? upc { get; set; }
        public string? alu { get; set; }
        public int maxdiscperc1 { get; set; }
        public int maxdiscperc2 { get; set; }

        public int tradediscpercent { get; set; }

        public string? activestoresid { get; set; }
        public string? activepricelevelsid { get; set; }
        public string? activeseasonsid { get; set; }

        public decimal actstrpricewt { get; set; }

        public decimal actstrprice { get; set; }
        public decimal actstrohqty { get; set; }
        public decimal actstrmarginpctg { get; set; }
        public decimal actstrmarginamt { get; set; }
        public decimal actstrmarginamtwt { get; set; }
        public decimal actstrmarkuppctg { get; set; }
        public decimal actstrcoefficient { get; set; }

        public string? dcscode { get; set; }

        public int kittype { get; set; }

        public int? ltypriceinpoints { get; set; }
        public int? ltypointsearned { get; set; }

        public List<UpdateInvnExtend> invnextend { get; set; } = new();
        public List<InvnPrice> invnprice { get; set; } = new();

        // ===============================
        // ✅ ADDED (CSV FULL MAPPING)
        // ===============================
        public string? vendor_code { get; set; }
        public string? vendor_name { get; set; }

        public decimal? order_cost { get; set; }
        public decimal? vendor_list_cost { get; set; }
        public decimal? trade_discount { get; set; }

        public int? case_qty { get; set; }
        public int? unit_per_case { get; set; }

        public string? tax_code { get; set; }

        public int? max_disc { get; set; }
        public int? acc_max_disc { get; set; }

        public string? dcs { get; set; }

        public string? dept_name { get; set; }
        public string? dept_code { get; set; }

        public string? class_name { get; set; }
        public string? class_code { get; set; }

        public string? subclass_name { get; set; }
        public string? subclass_code { get; set; }

        public bool? subloc_flag { get; set; }
        public int? qty_decimal { get; set; }
        public int? serialtype { get; set; }
        public int? lottype { get; set; }

        public string? regional_flag { get; set; }

        public DateTime? udf1date { get; set; }
        public DateTime? udf2date { get; set; }
        public DateTime? udf3date { get; set; }
    }

    public class InvnPrice
    {
        public decimal price { get; set; }
        public string? invnsbsitemsid { get; set; }
        public string? sbssid { get; set; }
        public string? pricelvlsid { get; set; }
        public string? seasonsid { get; set; }
        public string? udf4_string { get; set; }
    }

    public class ApiCallResult
    {
        public string PayloadJson { get; set; } = "";
        public string ResponseBody { get; set; } = "";
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; } = "";
    }

    public class UploadWorkItem
    {
        public string Key { get; set; } = "";
        public UploadData Data { get; set; } = new();
    }

    // ========= UPDATE-ONLY DTOs =========

    public class UpdateUploadRoot
    {
        public List<UpdateUploadData> data { get; set; } = new();
    }

    public class UpdateUploadData
    {
        public string OriginApplication { get; set; } = "";
        public UpdatePrimaryItemDefinition PrimaryItemDefinition { get; set; } = new();
        public List<UpdateInventoryItem> InventoryItems { get; set; } = new();

        public bool UpdateStyleDefinition { get; set; }
        public bool UpdateStyleCost { get; set; }
        public bool UpdateStyleLty { get; set; }
        public bool UpdateStylePrice { get; set; }


        public string? DefaultReasonSidForQtyMemo { get; set; }
        public string? DefaultReasonSidForCostMemo { get; set; }
        public string? DefaultReasonSidForPriceMemo { get; set; }
    }

    public class UpdatePrimaryItemDefinition
    {
        public string sid { get; set; } = "";
        public string dcssid { get; set; } = "";
        public string vendsid { get; set; } = "";
        public string description1 { get; set; } = "";
        public string description2 { get; set; } = "";
        public string attribute { get; set; } = "";
        public string itemsize { get; set; } = "";
    }

    public class UpdateInventoryItem
    {
        public string sid { get; set; } = "";
        public string sbssid { get; set; } = "";

        public string description1 { get; set; } = "";
        public string description2 { get; set; } = "";
        public string description3 { get; set; } = "";
        public string description4 { get; set; } = "";

        public string text1 { get; set; } = "";
        public string text2 { get; set; } = "";
        public string text3 { get; set; } = "";
        public string text4 { get; set; } = "";
        public string text5 { get; set; } = "";
        public string text6 { get; set; } = "";
        public string text7 { get; set; } = "";
        public string text8 { get; set; } = "";
        public string text9 { get; set; } = "";
        public string text10 { get; set; } = "";

        public string attribute { get; set; } = "";
        public string itemsize { get; set; } = "";

        public string udf1string { get; set; } = "";
        public string udf2string { get; set; } = "";
        public string udf3string { get; set; } = "";
        public string udf4string { get; set; } = "";
        public string udf5string { get; set; } = "";

        public string? upc { get; set; }
        public string? alu { get; set; }

        public int? kittype { get; set; }
        public decimal? ltypriceinpoints { get; set; }
        public decimal? ltypointsearned { get; set; }

        public string? activestoresid { get; set; }
        public string? activepricelevelsid { get; set; }
        public string? activeseasonsid { get; set; }

        public decimal? actstrpricewt { get; set; }
        public decimal? actstrmarginpctg { get; set; }
        public decimal? actstrmarginamt { get; set; }
        public decimal? actstrmarginamtwt { get; set; }
        public decimal? actstrmarkuppctg { get; set; }
        public decimal? actstrcoefficient { get; set; }

        public string? dcscode { get; set; }
        public string? dcssid { get; set; }

        public decimal? fstprice { get; set; }

        public string? taxcodesid { get; set; }

        public decimal? cost { get; set; }
        public decimal? spif { get; set; }

        public List<UpdateInvnExtend> invnextend { get; set; } = new();
        public List<UpdateInvnPrice> invnprice { get; set; } = new();

        // ===============================
        // ✅ ADDED (UPDATE PAYLOAD MATCH)
        // ===============================
        public decimal? order_cost { get; set; }
        public decimal? vendor_list_cost { get; set; }
        public decimal? trade_discount { get; set; }

        public int? case_qty { get; set; }
        public int? unit_per_case { get; set; }

        public int? qty_decimal { get; set; }

        public int? max_disc { get; set; }
        public int? acc_max_disc { get; set; }

        public string? tax_code { get; set; }

        public string? dcs { get; set; }

        public string? dept_name { get; set; }
        public string? dept_code { get; set; }

        public string? class_name { get; set; }
        public string? class_code { get; set; }

        public string? subclass_name { get; set; }
        public string? subclass_code { get; set; }

        public int? serialtype { get; set; }
        public int? lottype { get; set; }

        public string? vendor_code { get; set; }
        public string? vendor_name { get; set; }

        public DateTime? udf1date { get; set; }
        public DateTime? udf2date { get; set; }
        public DateTime? udf3date { get; set; }
        public int? noninventory { get; set; }

    }

    public class UpdateInvnExtend
    {
        public string sid { get; set; }

        public string udf6string { get; set; }
        public string udf7string { get; set; }
        public string udf8string { get; set; }
        public string udf9string { get; set; }
        public string udf10string { get; set; }
        public string udf11string { get; set; }
        public string udf12string { get; set; }
        public string udf13string { get; set; }
        public string udf14string { get; set; }
        public string udf15string { get; set; }

        public string? udf1largestring { get; set; }
        public string? udf2largestring { get; set; }

        public string invnsbsitemsid { get; set; }
    }

    public class UpdateInvnPrice
    {
        public string? sid { get; set; }
        public decimal price { get; set; }
        public string? invnsbsitemsid { get; set; }
        public string? sbssid { get; set; }
        public string? pricelvlsid { get; set; }
        public string? seasonsid { get; set; }
        public string? udf4_string { get; set; }
    }

    public class SbsTaxInfo
    {
        public string Sbssid { get; set; } = "";
        public string Taxcodesid { get; set; } = "";
    }

    public class ExistingItemInfo
    {
        public string? extend_sid { get; set; }
        public string? sid { get; set; }
        public string? alu { get; set; }
        public string? upc { get; set; }

        public string? description1 { get; set; }
        public string? description2 { get; set; }
        public string? description3 { get; set; }
        public string? description4 { get; set; }

        public string? attribute { get; set; }

        public string? item_size { get; set; }
        public decimal? cost { get; set; }

        public string? taxcodesid { get; set; }

        public string? udf1_string { get; set; }
        public string? udf2_string { get; set; }
        public string? udf3_string { get; set; }
        public string? udf4_string { get; set; }
        public string? udf5_string { get; set; }

        public string? udf6_string { get; set; }
        public string? udf7_string { get; set; }
        public string? udf8_string { get; set; }
        public string? udf9_string { get; set; }
        public string? udf10_string { get; set; }
        public string? udf11_string { get; set; }
        public string? udf12_string { get; set; }
        public string? udf13_string { get; set; }
        public string? udf14_string { get; set; }
        public string? udf15_string { get; set; }

        public decimal? spif { get; set; }

        public string? text1 { get; set; }
        public string? text2 { get; set; }
        public string? text3 { get; set; }
        public string? text4 { get; set; }
        public string? text5 { get; set; }
        public string? text6 { get; set; }
        public string? text7 { get; set; }
        public string? text8 { get; set; }
        public string? text9 { get; set; }
        public string? text10 { get; set; }

        public string? dcs_code { get; set; }
        public string? row_version { get; set; }

        public List<ExistingPriceInfo> Prices { get; set; } = new();

        // ===============================
        // ✅ ADDED (EXISTING LOOKUP SUPPORT)
        // ===============================
        public string? vendor_code { get; set; }
        public string? vendor_name { get; set; }

        public decimal? order_cost { get; set; }
        public decimal? vendor_list_cost { get; set; }
        public decimal? trade_discount { get; set; }

        public int? case_qty { get; set; }
        public int? unit_per_case { get; set; }

        public int? qty_decimal { get; set; }

        public int? max_disc { get; set; }
        public int? acc_max_disc { get; set; }

        public string? dept_name { get; set; }
        public string? dept_code { get; set; }

        public string? class_name { get; set; }
        public string? class_code { get; set; }

        public string? subclass_name { get; set; }
        public string? subclass_code { get; set; }

        public bool? subloc_flag { get; set; }
        public int? noninventory { get; set; }

        public string? udf1date { get; set; }
        public string? udf2date { get; set; }
        public string? udf3date { get; set; }

        public int? lottype { get; set; }
        public int? serialtype { get; set; }
    }

    public class ExistingPriceInfo
    {
        public string? sid { get; set; }
        public string? price_lvl_sid { get; set; }
        public int? price_lvl { get; set; }
        public decimal? price { get; set; }
        public string? seasonsid { get; set; }
        public string? sbssid { get; set; }
    }
}