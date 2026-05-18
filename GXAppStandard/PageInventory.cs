// PageInventory.cs
using GXUploader.Dtos;
using GXUploader.Helpers;
using Microsoft.VisualBasic.FileIO;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXUploader
{
    public partial class PageInventory : UserControl
    {
        private string _selectedCsvPath = string.Empty;

        private DataTable? _displayTable;
        private DataTable? _payloadTable;

        private PrismConfigRepository.PrismConfig? _prismCfg;

        // 0 = UPC, 1 = ALU
        private bool UseUpcMode => (_prismCfg?.ScanType ?? 0) == 0;

        private readonly string _connString;
        private const int OracleCommandTimeoutSeconds = 300;

        private static readonly HttpClient _http = new HttpClient(new HttpClientHandler { UseCookies = false })
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private readonly object _fileLogLock = new object();

        public PageInventory()
        {
            InitializeComponent();

            _connString = ConfigurationManager.AppSettings["OracleConnection"];

            _prismCfg = PrismConfigRepository.Load();

            ApplyTableBorder();
            btnStartUploading.Enabled = false;

            LogInfo($"Primary field: {(UseUpcMode ? "UPC" : "ALU")}");
        }

        private OracleConnection OpenOracleConnection()
        {
            OracleConnection.ClearAllPools();

            var conn = new OracleConnection(_connString);
            conn.Open();
            return conn;
        }

        private string BuildInventorySaveItemsUrl()
        {
            EnsurePrismConfigLoaded();

            string workstation = (_prismCfg?.Workstation ?? "").Trim();
            if (string.IsNullOrWhiteSpace(workstation))
                throw new Exception("Workstation not configured in prism_configuration.");
            return $"http://{workstation}/api/backoffice/inventory?action=InventorySaveItems";
        }

        private string BuildVendorUrl()
        {
            EnsurePrismConfigLoaded();

            string workstation = (_prismCfg?.Workstation ?? "").Trim();
            if (string.IsNullOrWhiteSpace(workstation))
                throw new Exception("Workstation not configured in prism_configuration.");

            return $"http://{workstation}/api/backoffice/vendor";
        }

        private string BuildUDFUrl()
        {
            EnsurePrismConfigLoaded();

            string workstation = (_prismCfg?.Workstation ?? "").Trim();
            if (string.IsNullOrWhiteSpace(workstation))
                throw new Exception("Workstation not configured in prism_configuration.");

            return $"http://{workstation}/api/backoffice/invnudfoption";
        }

        private void EnsurePrismConfigLoaded()
        {
            if (_prismCfg == null)
                _prismCfg = PrismConfigRepository.Load();
        }

        private async Task<string> GetAuthSessionAsync(bool forceRefresh = false)
        {
            return await PrismAuthSessionHelper.GetAuthSessionAsync(_http, forceRefresh).ConfigureAwait(false);
        }

        private static bool IsAuthExpiredStatus(HttpResponseMessage? resp)
        {
            if (resp == null) return false;
            return resp.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                   resp.StatusCode == System.Net.HttpStatusCode.Forbidden;
        }

        private string GetDailyFilePath(string folderName, string prefix, string ext)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
            Directory.CreateDirectory(dir);

            string fileName = $"{prefix}_{DateTime.Now:yyyyMMdd}.{ext}";
            return Path.Combine(dir, fileName);
        }

        private void AppendLineToFile(string fullPath, string line)
        {
            lock (_fileLogLock)
            {
                File.AppendAllText(fullPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        private string GetDailyApiLogPath()
            => GetDailyFilePath("ApiLogs", "InventoryApiLog", "txt");

        private void btnBrowseCsv_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select CSV File",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Multiselect = false
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                dgvInventory.DataSource = null;
                dgvInventory.Rows.Clear();
                dgvInventory.Columns.Clear();

                _displayTable = null;
                _payloadTable = null;

                btnStartUploading.Enabled = false;

                _selectedCsvPath = ofd.FileName;
                txtCsvPath.Text = Path.GetFileName(_selectedCsvPath);

                LogInfo($"Selected file: {txtCsvPath.Text} (previous data cleared)");
            }
        }

        private async void btnReadCsv_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedCsvPath) || !File.Exists(_selectedCsvPath))
            {
                LogWarn("Read CSV clicked but no valid file selected.");
                MessageBox.Show("Please browse and select a valid CSV file first.", "CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool allowCreateMissingData = false;

            var confirm = MessageBox.Show(
                "If some data does not exist in the database, do you want the system to create it automatically?\n\n" +
                "OK = Create missing data\n" +
                "Cancel = Read CSV only",
                "Create Missing Data",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.OK)
                allowCreateMissingData = true;

            try
            {
                LogInfo("Reading CSV (Row1=Payload header, Row2=Display header, Row3+=Data)...");

                ReadCsvToTwoTables(_selectedCsvPath, out _displayTable, out _payloadTable);

                _displayTable ??= new DataTable();
                _payloadTable ??= new DataTable();

                if (allowCreateMissingData)
                {
                    await UpdateVendorCodeFromDbAsync(_payloadTable);
                    // Remove 
                    // DO NOT call UpdateSbsNoFromDb here anymore because it overwrites SBS_No.
                    UpdateSbssidAndTaxcodesidFromDb(_payloadTable);

                    await UpdateDcsFromDbAsync(_payloadTable);
                    UpdatePriceSidFromDb(_payloadTable);
                    UpdateTextUdfSidFromDb(_payloadTable);
                }

                dgvInventory.DataSource = _displayTable;

                MakeGridReadable();
                HideEmptyColumns();
                ApplyTableBorder();

                btnStartUploading.Enabled = _payloadTable.Rows.Count > 0;
            }
            catch (OracleException oex)
            {
                btnStartUploading.Enabled = false;
                LogError($"Oracle error {oex.Number}: {oex.Message}");
                MessageBox.Show($"Oracle error {oex.Number}\n\n{oex.Message}", "Oracle Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                btnStartUploading.Enabled = false;
                LogError(ex.ToString());
                MessageBox.Show($"Failed to read CSV.\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //private async void btnReadCsv_Click(object sender, EventArgs e)
        //{
        //    if (string.IsNullOrWhiteSpace(_selectedCsvPath) || !File.Exists(_selectedCsvPath))
        //    {
        //        LogWarn("Read CSV clicked but no valid file selected.");
        //        MessageBox.Show("Please browse and select a valid CSV file first.", "CSV",
        //            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //        return;
        //    }

        //    bool allowCreateMissingData = false;

        //    var confirm = MessageBox.Show(
        //        "If some data does not exist in the database, do you want the system to create it automatically?\n\n" +
        //        "OK = Create missing data\n" +
        //        "Cancel = Read CSV only",
        //        "Create Missing Data",
        //        MessageBoxButtons.OKCancel,
        //        MessageBoxIcon.Question);

        //    if (confirm == DialogResult.OK)
        //        allowCreateMissingData = true;

        //    try
        //    {
        //        LogInfo("Reading CSV (Row1=Payload header, Row2=Display header, Row3+=Data)...");

        //        // -----------------------------
        //        // Load CSV
        //        // -----------------------------
        //        ReadCsvToTwoTables(_selectedCsvPath, out _displayTable, out _payloadTable);

        //        _displayTable ??= new DataTable();
        //        _payloadTable ??= new DataTable();

        //        int rowCount = _payloadTable.Rows.Count;

        //        // -----------------------------
        //        // Show grid ASAP (faster UX)
        //        // -----------------------------
        //        dgvInventory.SuspendLayout();
        //        dgvInventory.DataSource = _displayTable;
        //        MakeGridReadable();
        //        HideEmptyColumns();
        //        ApplyTableBorder();
        //        dgvInventory.ResumeLayout();

        //        btnStartUploading.Enabled = rowCount > 0;

        //        // -----------------------------
        //        // DB enrichment (OPTIMIZED)
        //        // -----------------------------
        //        if (allowCreateMissingData && rowCount > 0)
        //        {
        //            LogInfo("Enriching data from database...");

        //            // Run independent async tasks in parallel
        //            var vendorTask = UpdateVendorCodeFromDbAsync(_payloadTable);
        //            var dcsTask = UpdateDcsFromDbAsync(_payloadTable);

        //            // Synchronous but fast in-memory mapping
        //            UpdateSbssidAndTaxcodesidFromDb(_payloadTable);
        //            UpdatePriceSidFromDb(_payloadTable);
        //            UpdateTextUdfSidFromDb(_payloadTable);

        //            await Task.WhenAll(vendorTask, dcsTask);
        //        }

        //        LogInfo($"CSV processing completed. Rows: {rowCount}");
        //    }
        //    catch (OracleException oex)
        //    {
        //        btnStartUploading.Enabled = false;
        //        LogError($"Oracle error {oex.Number}: {oex.Message}");

        //        MessageBox.Show(
        //            $"Oracle error {oex.Number}\n\n{oex.Message}",
        //            "Oracle Error",
        //            MessageBoxButtons.OK,
        //            MessageBoxIcon.Error);
        //    }
        //    catch (Exception ex)
        //    {
        //        btnStartUploading.Enabled = false;
        //        LogError(ex.ToString());

        //        MessageBox.Show(
        //            $"Failed to read CSV.\n\n{ex.Message}",
        //            "Error",
        //            MessageBoxButtons.OK,
        //            MessageBoxIcon.Error);
        //    }
        //}

        private void ReadCsvToTwoTables(string filePath, out DataTable displayDt, out DataTable payloadDt)
        {
            displayDt = new DataTable();
            payloadDt = new DataTable();

            using var parser = new TextFieldParser(filePath);
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TrimWhiteSpace = true;

            if (parser.EndOfData) return;

            string[] payloadHeadersRaw = parser.ReadFields() ?? Array.Empty<string>();
            string[] displayHeadersRaw = parser.EndOfData ? Array.Empty<string>() : (parser.ReadFields() ?? Array.Empty<string>());

            int colCount = Math.Max(payloadHeadersRaw.Length, displayHeadersRaw.Length);
            if (colCount == 0) return;

            for (int i = 0; i < colCount; i++)
            {
                string h = i < payloadHeadersRaw.Length ? payloadHeadersRaw[i] : "";
                string colName = NormalizeHeaderOrFallback(h, $"Column{i + 1}");
                colName = MakeUniqueColumnName(payloadDt, colName);
                payloadDt.Columns.Add(colName);
            }

            for (int i = 0; i < colCount; i++)
            {
                string h = i < displayHeadersRaw.Length ? displayHeadersRaw[i] : "";
                if (string.IsNullOrWhiteSpace(h))
                    h = i < payloadHeadersRaw.Length ? payloadHeadersRaw[i] : "";

                string colName = NormalizeHeaderOrFallback(h, $"Column{i + 1}");
                colName = MakeUniqueColumnName(displayDt, colName);
                displayDt.Columns.Add(colName);
            }

            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields() ?? Array.Empty<string>();
                if (fields.Length < colCount) Array.Resize(ref fields, colCount);

                var prow = payloadDt.NewRow();
                var drow = displayDt.NewRow();

                for (int i = 0; i < colCount; i++)
                {
                    string val = fields[i] ?? "";
                    prow[i] = val;
                    drow[i] = val;
                }

                payloadDt.Rows.Add(prow);
                displayDt.Rows.Add(drow);
            }
        }

        private static string NormalizeHeaderOrFallback(string header, string fallback)
        {
            string h = (header ?? "").Trim();
            if (string.IsNullOrWhiteSpace(h)) return fallback;
            return h.Replace(" ", "_");
        }

        private string MakeUniqueColumnName(DataTable dt, string baseName)
        {
            string name = baseName;
            int counter = 1;
            while (dt.Columns.Contains(name))
            {
                counter++;
                name = $"{baseName}_{counter}";
            }
            return name;
        }

        //private async Task UpdateVendorCodeFromDbAsync(DataTable dt)
        //{
        //    if (dt == null)
        //        throw new ArgumentNullException(nameof(dt));

        //    if (dt.Rows.Count == 0)
        //    {
        //        LogWarn("UpdateVendorCodeFromDb: DataTable is empty.");
        //        return;
        //    }

        //    if (!dt.Columns.Contains("VENDOR_CODE"))
        //        throw new Exception("CSV column 'VENDOR_CODE' not found.");

        //    if (!dt.Columns.Contains("VENDOR_NAME"))
        //        throw new Exception("CSV column 'VENDOR_NAME' not found.");

        //    if (!dt.Columns.Contains("VENDOR_SID"))
        //        dt.Columns.Add("VENDOR_SID", typeof(string));

        //    using var conn = OpenOracleConnection();
        //    using var cmd = conn.CreateCommand();

        //    cmd.BindByName = true;
        //    cmd.CommandTimeout = OracleCommandTimeoutSeconds;
        //    cmd.CommandText = @"
        //        SELECT v.sid
        //        FROM RPS.VENDOR v
        //        WHERE v.VEND_CODE = :code
        //        FETCH FIRST 1 ROWS ONLY";

        //    cmd.Parameters.Add("code", OracleDbType.Varchar2);

        //    for (int i = 0; i < dt.Rows.Count; i++)
        //    {
        //        DataRow row = dt.Rows[i];
        //        string vendCode = row["VENDOR_CODE"]?.ToString()?.Trim() ?? string.Empty;
        //        string vendName = row["VENDOR_NAME"]?.ToString()?.Trim() ?? string.Empty;

        //        try
        //        {
        //            if (string.IsNullOrWhiteSpace(vendCode))
        //            {
        //                LogWarn($"[Row {i + 1}] VENDOR_CODE empty.");
        //                continue;
        //            }

        //            if (string.IsNullOrWhiteSpace(vendName))
        //                vendName = $"AUTO-{vendCode}";

        //            cmd.Parameters["code"].Value = vendCode;

        //            string sid = Convert.ToString(cmd.ExecuteScalar())?.Trim() ?? string.Empty;

        //            if (!string.IsNullOrWhiteSpace(sid))
        //            {
        //                row["VENDOR_SID"] = sid;
        //                LogInfo($"[Row {i + 1}] VENDOR FOUND → Code {vendCode}, SID {sid}");
        //                continue;
        //            }

        //            LogWarn($"[Row {i + 1}] Vendor not found. Creating... Code={vendCode}, Name={vendName}");

        //            await CreateVendorAsync(vendCode, vendName);

        //            cmd.Parameters["code"].Value = vendCode;
        //            sid = Convert.ToString(cmd.ExecuteScalar())?.Trim() ?? string.Empty;

        //            if (!string.IsNullOrWhiteSpace(sid))
        //            {
        //                row["VENDOR_SID"] = sid;
        //                LogInfo($"[Row {i + 1}] Vendor created → Code {vendCode}, SID {sid}");
        //            }
        //            else
        //            {
        //                LogError($"[Row {i + 1}] Vendor creation failed. Code={vendCode}");
        //            }
        //        }
        //        catch (OracleException ex)
        //        {
        //            LogError($"[Row {i + 1}] Oracle error while processing vendor. Code={vendCode}. {ex.Message}");
        //        }
        //        catch (Exception ex)
        //        {
        //            LogError($"[Row {i + 1}] Unexpected error while processing vendor. Code={vendCode}. {ex.Message}");
        //        }
        //    }

        //    LogInfo("Vendor lookup + creation completed.");
        //}

        private async Task UpdateVendorCodeFromDbAsync(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));

            if (dt.Rows.Count == 0)
            {
                LogWarn("UpdateVendorCodeFromDb: DataTable is empty.");
                return;
            }

            // Ensure required columns exist
            string[] requiredColumns = { "VENDOR_CODE", "VENDOR_NAME", "SBS_No." };
            foreach (var col in requiredColumns)
            {
                if (!dt.Columns.Contains(col))
                    throw new Exception($"CSV column '{col}' not found.");
            }

            if (!dt.Columns.Contains("VENDOR_SID"))
                dt.Columns.Add("VENDOR_SID", typeof(string));

            using var conn = OpenOracleConnection();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow row = dt.Rows[i];
                string vendCode = row["VENDOR_CODE"]?.ToString()?.Trim() ?? string.Empty;
                string vendName = row["VENDOR_NAME"]?.ToString()?.Trim() ?? string.Empty;
                string sbsNo = row["SBS_No."]?.ToString()?.Trim() ?? string.Empty;

                try
                {
                    if (string.IsNullOrWhiteSpace(vendCode))
                    {
                        LogWarn($"[Row {i + 1}] VENDOR_CODE empty. Skipped.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(vendName))
                        vendName = $"AUTO-{vendCode}";

                    string sid = "";
                    string subsidiarySid = "";

                    // STEP 0: Lookup subsidiary SID from SBS_No.
                    using (var subCmd = new OracleCommand(@"
                        SELECT sid 
                        FROM rps.subsidiary
                        WHERE sbs_no = :sbsno
                        FETCH FIRST 1 ROWS ONLY", conn))
                    {
                        subCmd.BindByName = true;
                        subCmd.CommandTimeout = OracleCommandTimeoutSeconds;
                        subCmd.Parameters.Add(":sbsno", OracleDbType.Varchar2).Value = sbsNo;

                        subsidiarySid = subCmd.ExecuteScalar()?.ToString()?.Trim() ?? "";
                    }

                    if (string.IsNullOrWhiteSpace(subsidiarySid))
                    {
                        LogError($"[Row {i + 1}] Subsidiary not found → SBS_No={sbsNo}");
                        continue;
                    }

                    // STEP 1: Lookup vendor SID by code + subsidiary
                    using (var cmd = new OracleCommand(@"
                        SELECT v.sid
                        FROM rps.vendor v
                        WHERE v.vend_code = :code
                          AND v.sbs_sid = :subsidiarySid
                        FETCH FIRST 1 ROWS ONLY", conn))
                    {
                        cmd.BindByName = true;
                        cmd.CommandTimeout = OracleCommandTimeoutSeconds;
                        cmd.Parameters.Add(":code", OracleDbType.Varchar2).Value = vendCode;
                        cmd.Parameters.Add(":subsidiarySid", OracleDbType.Varchar2).Value = subsidiarySid;

                        sid = cmd.ExecuteScalar()?.ToString()?.Trim() ?? "";
                    }

                    // STEP 2: If not found, create vendor
                    if (string.IsNullOrWhiteSpace(sid))
                    {
                        //LogWarn($"[Row {i + 1}] Vendor not found. Creating... Code={vendCode}, Name={vendName}");

                        // Prepare API payload with correct subsidiary SID
                        var apiPayload = new
                        {
                            data = new[]
                            {
                                new
                                {
                                    originapplication = "RProPrismWeb",
                                    active = true,
                                    vendorterm = Array.Empty<object>(),
                                    vendoraddress = Array.Empty<object>(),
                                    vendorcontact = Array.Empty<object>(),
                                    sbssid = subsidiarySid,
                                    regional = false,
                                    vendcode = vendCode,
                                    vendname = vendName
                                }
                            }
                        };

                        string payloadJson = JsonSerializer.Serialize(apiPayload);

                        // Write payload before creation
                        //WriteApiPayloadResponseToDailyFile(vendCode, payloadJson, "CREATING");

                        // Create vendor
                        await CreateVendorAsync(vendCode, vendName, subsidiarySid);

                        // STEP 3: Re-fetch SID after creation
                        using (var cmd2 = new OracleCommand(@"
                            SELECT v.sid
                            FROM rps.vendor v
                            WHERE v.vend_code = :code
                              AND v.sbs_sid = :subsidiarySid
                            FETCH FIRST 1 ROWS ONLY", conn))
                        {
                            cmd2.BindByName = true;
                            cmd2.CommandTimeout = OracleCommandTimeoutSeconds;
                            cmd2.Parameters.Add(":code", OracleDbType.Varchar2).Value = vendCode;
                            cmd2.Parameters.Add(":subsidiarySid", OracleDbType.Varchar2).Value = subsidiarySid;

                            sid = cmd2.ExecuteScalar()?.ToString()?.Trim() ?? "";
                        }

                        if (!string.IsNullOrWhiteSpace(sid))
                        {
                            row["VENDOR_SID"] = sid;
                            //LogInfo($"[Row {i + 1}] Vendor created → Code={vendCode}, SID={sid}");

                            WriteApiPayloadResponseToDailyFile(vendCode, payloadJson, $"CREATED SID={sid}");
                        }
                        else
                        {
                            //LogError($"[Row {i + 1}] Vendor creation failed → Code={vendCode}");
                            WriteApiPayloadResponseToDailyFile(vendCode, payloadJson, "CREATION FAILED");
                        }
                    }
                    else
                    {
                        // Vendor found
                        row["VENDOR_SID"] = sid;
                        //LogInfo($"[Row {i + 1}] VENDOR FOUND → Code={vendCode}, SID={sid}");
                    }
                }
                catch (OracleException ex)
                {
                    LogError($"[Row {i + 1}] Oracle error while processing vendor Code={vendCode}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    LogError($"[Row {i + 1}] Unexpected error while processing vendor Code={vendCode}: {ex.Message}");
                }
            }

            LogInfo("Vendor lookup + creation completed.");
        }

        private async Task UpdateDcsFromDbAsync(DataTable dt)
        {
            if (dt.Rows.Count == 0)
            {
                LogWarn("[DCS] DataTable is empty.");
                return;
            }

            string[] requiredColumns =
            {
                "DCS",
                "DEPT_NAME",
                "DEPT_CODE",
                "CLASS_NAME",
                "CLASS_CODE",
                "SUBCLASS_NAME",
                "SUBCLASS_CODE",
                "SBS_No."
            };

            foreach (var col in requiredColumns)
            {
                if (!dt.Columns.Contains(col))
                    throw new Exception($"CSV column '{col}' not found.");
            }

            if (!dt.Columns.Contains("DCS_CODE"))
                dt.Columns.Add("DCS_CODE", typeof(string));

            using var conn = OpenOracleConnection();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                try
                {
                    string deptName = dt.Rows[i]["DEPT_NAME"]?.ToString()?.Trim() ?? "";
                    string deptCode = dt.Rows[i]["DEPT_CODE"]?.ToString()?.Trim() ?? "";
                    string className = dt.Rows[i]["CLASS_NAME"]?.ToString()?.Trim() ?? "";
                    string classCode = dt.Rows[i]["CLASS_CODE"]?.ToString()?.Trim() ?? "";
                    string subClassName = dt.Rows[i]["SUBCLASS_NAME"]?.ToString()?.Trim() ?? "";
                    string subClassCode = dt.Rows[i]["SUBCLASS_CODE"]?.ToString()?.Trim() ?? "";
                    string sbsNo = dt.Rows[i]["SBS_No."]?.ToString()?.Trim() ?? "";

                    // STEP 1: CSV DCS value → store as DCS_CODE
                    string csvDcsValue = dt.Rows[i]["DCS"]?.ToString()?.Trim() ?? "";
                    string dcsCode = dt.Rows[i]["DCS_CODE"]?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(dcsCode))
                    {
                        if (!string.IsNullOrWhiteSpace(csvDcsValue))
                            dcsCode = csvDcsValue;
                        else
                            dcsCode = $"{deptCode}{classCode}{subClassCode}".Trim();

                        dt.Rows[i]["DCS_CODE"] = dcsCode;
                        //LogWarn($"[Row {i + 1}] Set DCS_CODE={dcsCode}");
                    }

                    if (string.IsNullOrWhiteSpace(dcsCode))
                    {
                        //LogWarn($"[Row {i + 1}] No CODE available. Skipped.");
                        continue;
                    }

                    string sid = "";

                    // STEP 2: Lookup SID by CODE + SBS_No
                    using (var cmd = new OracleCommand(
                        @"SELECT d.sid
                          FROM rps.subsidiary s
                          LEFT JOIN rps.dcs d ON d.sbs_sid = s.sid
                          WHERE d.dcs_code = :code
                            AND s.sbs_no = :sbsno
                          FETCH FIRST 1 ROWS ONLY",
                        conn))
                    {
                        cmd.BindByName = true;
                        cmd.CommandTimeout = OracleCommandTimeoutSeconds;
                        cmd.Parameters.Add(":code", OracleDbType.Varchar2).Value = dcsCode;
                        cmd.Parameters.Add(":sbsno", OracleDbType.Varchar2).Value = sbsNo;

                        var result = cmd.ExecuteScalar();
                        sid = (result != null) ? result.ToString().Trim() : ""; // ✅ ensure empty string if null
                    }

                    // Always store SID (or empty) in CSV
                    dt.Rows[i]["DCS"] = sid;

                    //if (!string.IsNullOrWhiteSpace(sid))
                        //LogInfo($"[Row {i + 1}] [DCS] FOUND → CODE={dcsCode}, SID={sid}");
                    //else
                        //LogWarn($"[Row {i + 1}] [DCS] NOT FOUND → CODE={dcsCode}, DCS set to empty");

                    if (string.IsNullOrWhiteSpace(deptCode) &&
                        string.IsNullOrWhiteSpace(classCode) &&
                        string.IsNullOrWhiteSpace(subClassCode))
                    {
                        //LogWarn($"[Row {i + 1}] Cannot create (no structure). Skipped.");
                        continue;
                    }

                    //LogWarn($"[Row {i + 1}] [DCS] NOT FOUND → Creating CODE={dcsCode}");

                    string subsidiarySid = PrismAuthSessionHelper.SubsidiarySid;
                    if (string.IsNullOrWhiteSpace(subsidiarySid))
                    {
                        await GetAuthSessionAsync();
                        subsidiarySid = PrismAuthSessionHelper.SubsidiarySid;
                    }

                    EnsurePrismConfigLoaded();
                    string workstation = (_prismCfg?.Workstation ?? "").Trim();
                    string url = $"http://{workstation}/api/backoffice/dcs";
                    string auth = await GetAuthSessionAsync();

                    // Fetch TAXABLE sid dynamically per row
                    string taxCodeSid = "";
                    string csvTaxCode = dt.Rows[i]["TAX_CODE"]?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(csvTaxCode))
                    {
                        throw new Exception($"Row {i + 1}: TAX_CODE not found in CSV.");
                    }

                    using (var taxCmd = new OracleCommand(
                        @"SELECT t.sid
                          FROM rps.subsidiary s
                          LEFT JOIN rps.tax_code t ON t.sbs_sid = s.sid
                          WHERE t.tax_code = :taxCode
                            AND s.sbs_no = :sbsno
                          FETCH FIRST 1 ROWS ONLY",
                        conn))
                    {
                        taxCmd.BindByName = true;
                        taxCmd.CommandTimeout = OracleCommandTimeoutSeconds;
                        taxCmd.Parameters.Add(":taxCode", OracleDbType.Varchar2).Value = csvTaxCode;
                        taxCmd.Parameters.Add(":sbsno", OracleDbType.Varchar2).Value = sbsNo;

                        taxCodeSid = taxCmd.ExecuteScalar()?.ToString()?.Trim() ?? "";
                    }

                    if (string.IsNullOrWhiteSpace(taxCodeSid))
                        throw new Exception($"Row {i + 1}: taxcodesid not found for TAX_CODE={csvTaxCode}");

                    var payload = new
                    {
                        data = new[]
                        {
                            new
                            {
                                originapplication = "RProPrismWeb",
                                active = 1,
                                sbssid = subsidiarySid,
                                regional = false,
                                d = deptCode,
                                c = classCode,
                                s = subClassCode,
                                dcscode = dcsCode,
                                dname = deptName,
                                cname = className,
                                sname = subClassName,
                                taxcodesid = taxCodeSid
                            }
                        }
                    };

                    string json = JsonSerializer.Serialize(payload);

                    using var req = new HttpRequestMessage(HttpMethod.Post, url);
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    req.Headers.TryAddWithoutValidation("auth-session", auth);
                    req.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, version=2");

                    using var resp = await _http.SendAsync(req);
                    var body = await resp.Content.ReadAsStringAsync();

                    WriteApiPayloadResponseToDailyFile(dcsCode, json, body);

                    if (!resp.IsSuccessStatusCode)
                    {
                        LogError(body);
                        continue;
                    }

                    // STEP 3: Re-fetch SID after creation
                    using (var cmd2 = new OracleCommand(
                        @"SELECT d.sid
                          FROM rps.subsidiary s
                          LEFT JOIN rps.dcs d ON d.sbs_sid = s.sid
                          WHERE d.dcs_code = :code
                            AND s.sbs_no = :sbsno
                          FETCH FIRST 1 ROWS ONLY",
                        conn))
                    {
                        cmd2.BindByName = true;
                        cmd2.Parameters.Add(":code", OracleDbType.Varchar2).Value = dcsCode;
                        cmd2.Parameters.Add(":sbsno", OracleDbType.Varchar2).Value = sbsNo;

                        sid = cmd2.ExecuteScalar()?.ToString()?.Trim() ?? "";
                    }

                    if (!string.IsNullOrWhiteSpace(sid))
                    {
                        dt.Rows[i]["DCS"] = sid;
                        //LogInfo($"[Row {i + 1}] [DCS] CREATED → CODE={dcsCode}, SID={sid}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[Row {i + 1}] [DCS] Error: {ex.Message}");
                }
            }

            LogInfo("[DCS] Lookup completed");
        }

        private void UpdatePriceSidFromDb(DataTable payloadDt)
        {
            if (payloadDt.Rows.Count == 0)
            {
                LogWarn("[PRICE_SID] Payload DataTable is empty.");
                return;
            }

            using var conn = OpenOracleConnection();

            var levelIndexes = new List<int>();

            foreach (DataColumn col in payloadDt.Columns)
            {
                var name = (col.ColumnName ?? "").Trim();
                if (!name.StartsWith("PRICE_LEVEL", StringComparison.OrdinalIgnoreCase))
                    continue;

                var suffix = name.Substring("PRICE_LEVEL".Length);
                if (!int.TryParse(suffix, out int idx))
                    continue;

                levelIndexes.Add(idx);
            }

            levelIndexes = levelIndexes.Distinct().OrderBy(x => x).ToList();

            if (levelIndexes.Count == 0)
            {
                LogWarn("[PRICE_SID] No PRICE_LEVEL{n} columns found.");
                return;
            }

            // Ensure PRICE_SID columns exist
            foreach (int idx in levelIndexes)
            {
                string sidCol = $"PRICE_SID{idx}";
                if (!payloadDt.Columns.Contains(sidCol))
                    payloadDt.Columns.Add(sidCol, typeof(string));
            }

            // 🔥 PROCESS PER ROW (SBS_NO IS PER ROW)
            for (int r = 0; r < payloadDt.Rows.Count; r++)
            {
                var row = payloadDt.Rows[r];

                string sbsNoText = row["SBS_No."]?.ToString()?.Trim();

                if (!int.TryParse(sbsNoText, out int sbsNo))
                {
                    LogWarn($"[PRICE_SID][Row {r + 1}] Invalid SBS_No.: {sbsNoText}");
                    continue;
                }

                var lvlToSid = new Dictionary<int, string>();

                string inParams = string.Join(",", levelIndexes.Select((x, i) => $":p{i}"));

                string sql = $@"
                    SELECT pl.price_lvl, pl.sid 
                    FROM rps.price_level pl
                    LEFT JOIN rps.subsidiary s ON s.sid = pl.sbs_sid
                    WHERE pl.price_lvl IN ({inParams})
                    AND s.sbs_no = :sbsNo";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.CommandTimeout = OracleCommandTimeoutSeconds;

                    for (int i = 0; i < levelIndexes.Count; i++)
                        cmd.Parameters.Add($":p{i}", OracleDbType.Int32).Value = levelIndexes[i];

                    cmd.Parameters.Add(":sbsNo", OracleDbType.Int32).Value = sbsNo;

                    using var rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        if (!int.TryParse(rdr["price_lvl"]?.ToString()?.Trim(), out int lvl))
                            continue;

                        string sid = rdr["sid"]?.ToString()?.Trim() ?? "";

                        if (!lvlToSid.ContainsKey(lvl))
                            lvlToSid.Add(lvl, sid);
                    }
                }

                // Assign values per level
                foreach (int idx in levelIndexes)
                {
                    string levelCol = $"PRICE_LEVEL{idx}";
                    string sidCol = $"PRICE_SID{idx}";

                    string priceText = row[levelCol]?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(priceText))
                    {
                        row[sidCol] = DBNull.Value;
                        continue;
                    }

                    if (lvlToSid.TryGetValue(idx, out string sid) && !string.IsNullOrWhiteSpace(sid))
                    {
                        row[sidCol] = sid;
                    }
                    else
                    {
                        row[sidCol] = DBNull.Value;
                        //LogWarn($"[PRICE_SID][Row {r + 1}] price_lvl {idx} not found for SBS {sbsNo}");
                    }
                }
            }

            LogInfo($"[PRICE_SID] Done. SBS-based mapping applied per row.");
        }

        private List<string> ExtractApiErrorMessages(string responseBody)
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(responseBody)) return messages;

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (!root.TryGetProperty("errors", out var errorsProp))
                    return messages;

                if (errorsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var err in errorsProp.EnumerateArray())
                    {
                        if (err.TryGetProperty("errormsg", out var msgProp))
                        {
                            string? msg = msgProp.GetString();
                            if (!string.IsNullOrWhiteSpace(msg))
                                messages.Add(msg);
                        }
                    }
                }
                else if (errorsProp.ValueKind == JsonValueKind.String)
                {
                    string? raw = errorsProp.GetString();
                    if (!string.IsNullOrWhiteSpace(raw) && raw.Trim().StartsWith("["))
                    {
                        using var innerDoc = JsonDocument.Parse(raw);
                        foreach (var err in innerDoc.RootElement.EnumerateArray())
                        {
                            if (err.TryGetProperty("errormsg", out var msgProp))
                            {
                                string? msg = msgProp.GetString();
                                if (!string.IsNullOrWhiteSpace(msg))
                                    messages.Add(msg);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return messages;
        }

        //private async void btnStartUploading_Click(object sender, EventArgs e)
        //{
        //    if (_payloadTable == null || _payloadTable.Rows.Count == 0)
        //    {
        //        LogWarn("No payload data loaded. Please Read CSV first.");
        //        return;
        //    }

        //    btnStartUploading.Enabled = false;
        //    btnReadCsv.Enabled = false;
        //    btnBrowseCsv.Enabled = false;

        //    bool stopped = false;

        //    try
        //    {
        //        await GetAuthSessionAsync(forceRefresh: false);

        //        if (string.IsNullOrWhiteSpace(PrismAuthSessionHelper.SeasonSid))
        //        {
        //            LogWarn("SeasonSid is empty. Forcing Prism auth refresh once...");
        //            PrismAuthSessionHelper.ClearCache();
        //            await GetAuthSessionAsync(forceRefresh: true);
        //        }

        //        if (string.IsNullOrWhiteSpace(PrismAuthSessionHelper.SeasonSid))
        //            throw new Exception("SeasonSid is still empty. Prism session did not return seasonsid.");

        //        // Refresh SBS, TAX, DCS
        //        UpdateSbssidAndTaxcodesidFromDb(_payloadTable);
        //        await UpdateDcsFromDbAsync(_payloadTable);

        //        var allWork = BuildUploadWorkItemsFromDataTable(_payloadTable);

        //        if (allWork.Count == 0)
        //        {
        //            LogWarn("No valid rows to upload.");
        //            return;
        //        }

        //        int inserted = 0;
        //        int updated = 0;
        //        int skipped = 0;
        //        int processed = 0;
        //        int errorCount = 0;

        //        foreach (var work in allWork)
        //        {
        //            string itemCode = GetItemCodeFromWorkItem(work)?.Trim();

        //            if (string.IsNullOrWhiteSpace(itemCode))
        //            {
        //                LogWarn($"Skipping row: item code is empty. Key={work.Key}");
        //                processed++;
        //                continue;
        //            }

        //            // ✅ SBS from work (IMPORTANT)
        //            string sbsNo = work.Data.InventoryItems[0].sbssid?.Trim();
        //            if (string.IsNullOrWhiteSpace(sbsNo))
        //            {
        //                LogWarn($"Skipping item {itemCode}: SBS_NO is empty. Key={work.Key}");
        //                processed++;
        //                continue;
        //            }

        //            //LogInfo($"Checking item: {itemCode} | SBS={sbsNo} | Key={work.Key}");
        //            //LogInfo($"Checking item: {sbsNo}");
        //            var existing = await GetExistingItemInfoAsync(itemCode, UseUpcMode, sbsNo);
        //            object payloadToSend;
        //            string actionLabel;

        //            if (existing == null)
        //            {
        //                payloadToSend = new UploadRoot
        //                {
        //                    data = new List<UploadData> { work.Data }
        //                };
        //                actionLabel = "INSERT";
        //            }
        //            else if (HasItemChanges(work, existing))
        //            {
        //                payloadToSend = BuildUpdatePayload(work, existing);
        //                actionLabel = "UPDATE";
        //            }
        //            else
        //            {
        //                skipped++;
        //                processed++;
        //                LogInfo($"SKIPPED (no changes): {itemCode} | SBS={sbsNo} | Key={work.Key}");
        //                continue;
        //            }

        //            //LogInfo($"POST {actionLabel}: {itemCode} | SBS={sbsNo}");

        //            var result = await PostInventorySaveItemsAsync(payloadToSend);

        //            WriteApiPayloadResponseToDailyFile(work.Key, result.PayloadJson, result.ResponseBody);

        //            var apiErrors = ExtractApiErrorMessages(result.ResponseBody);

        //            bool hasErrors =
        //                result.StatusCode < 200 ||
        //                result.StatusCode >= 300 ||
        //                (apiErrors != null && apiErrors.Count > 0);

        //            if (hasErrors)
        //            {
        //                errorCount++;

        //                string errorMessage = (apiErrors != null && apiErrors.Count > 0)
        //                    ? string.Join(" | ", apiErrors)
        //                    : result.ResponseBody;

        //                //LogError($"[UPLOAD ERROR #{errorCount}] Item={itemCode} | SBS={sbsNo} | Error={errorMessage}");

        //                processed++;
        //                LogInfo($"Progress: {processed}/{allWork.Count} | Errors: {errorCount}");

        //                continue;
        //            }

        //            if (actionLabel == "INSERT")
        //                inserted++;
        //            else if (actionLabel == "UPDATE")
        //                updated++;

        //            processed++;
        //            LogInfo($"Progress: {processed}/{allWork.Count}");
        //        }

        //        if (!stopped)
        //        {
        //            MessageBox.Show(
        //                $"Upload completed.\n\nInserted: {inserted}\nUpdated: {updated}\nSkipped: {skipped}\nError: {errorCount}",
        //                "Done",
        //                MessageBoxButtons.OK,
        //                MessageBoxIcon.Information);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogError($"Upload failed: {ex}");
        //        MessageBox.Show(ex.Message, "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //    finally
        //    {
        //        btnStartUploading.Enabled = true;
        //        btnReadCsv.Enabled = true;
        //        btnBrowseCsv.Enabled = true;
        //    }
        //}

        private async void btnStartUploading_Click(object sender, EventArgs e)
        {
            if (_payloadTable == null || _payloadTable.Rows.Count == 0)
            {
                LogWarn("No payload data loaded. Please Read CSV first.");
                return;
            }

            btnStartUploading.Enabled = false;
            btnReadCsv.Enabled = false;
            btnBrowseCsv.Enabled = false;

            bool stopped = false;

            try
            {
                await GetAuthSessionAsync(forceRefresh: false);

                if (string.IsNullOrWhiteSpace(PrismAuthSessionHelper.SeasonSid))
                {
                    LogWarn("SeasonSid is empty. Forcing Prism auth refresh once...");
                    PrismAuthSessionHelper.ClearCache();
                    await GetAuthSessionAsync(forceRefresh: true);
                }

                if (string.IsNullOrWhiteSpace(PrismAuthSessionHelper.SeasonSid))
                    throw new Exception("SeasonSid is still empty. Prism session did not return seasonsid.");

                UpdateSbssidAndTaxcodesidFromDb(_payloadTable);
                await UpdateDcsFromDbAsync(_payloadTable);

                var allWork = BuildUploadWorkItemsFromDataTable(_payloadTable);

                if (allWork.Count == 0)
                {
                    LogWarn("No valid rows to upload.");
                    return;
                }

                int inserted = 0;
                int updated = 0;
                int skipped = 0;
                int processed = 0;
                int errorCount = 0;

                var semaphore = new SemaphoreSlim(10); // 🔥 SPEED CONTROL (ONLY CHANGE)

                var tasks = new List<Task>();

                foreach (var work in allWork)
                {
                    await semaphore.WaitAsync();

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            string itemCode = GetItemCodeFromWorkItem(work)?.Trim();

                            if (string.IsNullOrWhiteSpace(itemCode))
                            {
                                int current = Interlocked.Increment(ref processed);
                                LogWarn($"Progress: {current}/{allWork.Count} | Skipping row: item code empty | Key={work.Key}");
                                return;
                            }

                            string sbsNo = work.Data.InventoryItems[0].sbssid?.Trim();

                            if (string.IsNullOrWhiteSpace(sbsNo))
                            {
                                int current = Interlocked.Increment(ref processed);
                                LogWarn($"Progress: {current}/{allWork.Count} | Skipping SBS empty | Item={itemCode} | Key={work.Key}");
                                return;
                            }

                            var existing = await GetExistingItemInfoAsync(itemCode, UseUpcMode, sbsNo);

                            object payloadToSend;
                            string actionLabel;

                            if (existing == null)
                            {
                                payloadToSend = new UploadRoot
                                {
                                    data = new List<UploadData> { work.Data }
                                };
                                actionLabel = "INSERT";
                            }
                            else if (HasItemChanges(work, existing))
                            {
                                payloadToSend = BuildUpdatePayload(work, existing);
                                actionLabel = "UPDATE";
                            }
                            else
                            {
                                int current = Interlocked.Increment(ref processed);
                                Interlocked.Increment(ref skipped);

                                LogInfo($"Progress: {current}/{allWork.Count} | SKIPPED (no changes) | Item={itemCode} | Key={work.Key}");
                                return;
                            }

                            var result = await PostInventorySaveItemsAsync(payloadToSend);
                            WriteApiPayloadResponseToDailyFile(work.Key, result.PayloadJson, result.ResponseBody);

                            var apiErrors = ExtractApiErrorMessages(result.ResponseBody);

                            bool hasErrors =
                                result.StatusCode < 200 ||
                                result.StatusCode >= 300 ||
                                (apiErrors != null && apiErrors.Count > 0);

                            if (hasErrors)
                            {
                                Interlocked.Increment(ref errorCount);

                                int current = Interlocked.Increment(ref processed);

                                LogInfo($"Progress: {current}/{allWork.Count} | ERROR | Item={itemCode} | Key={work.Key}");
                                return;
                            }

                            if (actionLabel == "INSERT")
                                Interlocked.Increment(ref inserted);
                            else if (actionLabel == "UPDATE")
                                Interlocked.Increment(ref updated);

                            int success = Interlocked.Increment(ref processed);

                            LogInfo($"Progress: {success}/{allWork.Count} | {actionLabel} | Item={itemCode} | Key={work.Key}");
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref errorCount);

                            int current = Interlocked.Increment(ref processed);

                            LogError($"Progress: {current}/{allWork.Count} | ERROR | Key={work.Key} | {ex}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                // adjustment Memo price level only 




                if (!stopped)
                {
                    MessageBox.Show(
                        $"Upload completed.\n\nInserted: {inserted}\nUpdated: {updated}\nSkipped: {skipped}\nError: {errorCount}",
                        "Done",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogError($"Upload failed: {ex}");
                MessageBox.Show(ex.Message, "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStartUploading.Enabled = true;
                btnReadCsv.Enabled = true;
                btnBrowseCsv.Enabled = true;
            }
        }

        private void WriteApiPayloadResponseToDailyFile(string upcOrAlu, string payloadJson, string responseBody)
        {
            try
            {
                string path = GetDailyApiLogPath();

                string key = string.IsNullOrWhiteSpace(upcOrAlu) ? "UNKNOWN" : upcOrAlu.Trim();
                string date = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");

                AppendLineToFile(path, $"[{date}][{key}][apiPayload] - {payloadJson}");
                AppendLineToFile(path, $"[{date}][{key}][apiResponse] - {responseBody}");
                AppendLineToFile(path, "");

                //LogInfo($"API payload+response appended to daily API log: {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to write daily API log: {ex.Message}");
            }
        }

        private async Task<ApiCallResult> PostInventorySaveItemsAsync(object payload)
        {
            string url;
            try
            {
                url = BuildInventorySaveItemsUrl();
            }
            catch (Exception ex)
            {
                LogError($"URL build failed: {ex.Message}");

                return new ApiCallResult
                {
                    PayloadJson = "",
                    ResponseBody = $"URL build failed: {ex.Message}",
                    StatusCode = 0,
                    ReasonPhrase = "URL Build Failed"
                };
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string json = JsonSerializer.Serialize(payload, options);

            try
            {
                string auth = await GetAuthSessionAsync().ConfigureAwait(false);

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                req.Headers.TryAddWithoutValidation("auth-session", auth);
                req.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, version=2");

                using var resp = await _http.SendAsync(req).ConfigureAwait(false);
                string rawBody = resp.Content != null
                    ? await resp.Content.ReadAsStringAsync().ConfigureAwait(false)
                    : string.Empty;

                if (IsAuthExpiredStatus(resp))
                {
                    LogWarn($"Auth expired (HTTP {(int)resp.StatusCode}). Refreshing auth-session and retrying once...");

                    PrismAuthSessionHelper.ClearCache();
                    string auth2 = await GetAuthSessionAsync(forceRefresh: true).ConfigureAwait(false);

                    using var retryReq = new HttpRequestMessage(HttpMethod.Post, url);
                    retryReq.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    retryReq.Headers.TryAddWithoutValidation("auth-session", auth2);
                    retryReq.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, version=2");

                    using var retryResp = await _http.SendAsync(retryReq).ConfigureAwait(false);
                    string retryBody = retryResp.Content != null
                        ? await retryResp.Content.ReadAsStringAsync().ConfigureAwait(false)
                        : string.Empty;

                    var retryErrors = ExtractApiErrorMessages(retryBody);
                    string finalRetryBody = retryErrors.Count > 0
                        ? string.Join(Environment.NewLine, retryErrors)
                        : retryBody;

                    LogInfo($"POST {url} (retry)");
                    LogInfo($"HTTP {(int)retryResp.StatusCode} {retryResp.ReasonPhrase}");

                    if (!retryResp.IsSuccessStatusCode)
                    {
                        if (!string.IsNullOrWhiteSpace(finalRetryBody))
                            LogError(finalRetryBody);
                        else
                            LogError("Request failed but response body was empty.");
                    }

                    return new ApiCallResult
                    {
                        PayloadJson = json,
                        ResponseBody = finalRetryBody,
                        StatusCode = (int)retryResp.StatusCode,
                        ReasonPhrase = retryResp.ReasonPhrase ?? ""
                    };
                }

                var apiErrors = ExtractApiErrorMessages(rawBody);

                string finalBody = apiErrors.Count > 0
                    ? string.Join(Environment.NewLine, apiErrors)
                    : rawBody;

                LogInfo($"POST {url}");
                LogInfo($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");

                if (!resp.IsSuccessStatusCode)
                {
                    if (!string.IsNullOrWhiteSpace(finalBody))
                        LogError(finalBody);
                    else
                        LogError("Request failed but response body was empty.");
                }

                return new ApiCallResult
                {
                    PayloadJson = json,
                    ResponseBody = finalBody,
                    StatusCode = (int)resp.StatusCode,
                    ReasonPhrase = resp.ReasonPhrase ?? ""
                };
            }
            catch (TaskCanceledException ex)
            {
                LogError($"Request timeout. POST {url}. {ex.Message}");

                return new ApiCallResult
                {
                    PayloadJson = json,
                    ResponseBody = $"Request timeout: {ex.Message}",
                    StatusCode = 0,
                    ReasonPhrase = "Timeout"
                };
            }
            catch (HttpRequestException ex)
            {
                LogError($"HTTP request error. POST {url}. {ex.Message}");

                return new ApiCallResult
                {
                    PayloadJson = json,
                    ResponseBody = $"HTTP request error: {ex.Message}",
                    StatusCode = 0,
                    ReasonPhrase = "HttpRequestException"
                };
            }
            catch (Exception ex)
            {
                LogError($"Unexpected error. POST {url}. {ex}");

                return new ApiCallResult
                {
                    PayloadJson = json,
                    ResponseBody = $"Unexpected error: {ex.Message}",
                    StatusCode = 0,
                    ReasonPhrase = "Exception"
                };
            }
        }

        private DateTime? ParseUdfUtcDate(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (DateTime.TryParse(
                value.ToString(),
                null,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out DateTime result))
            {
                return result; // stays UTC
            }

            return null;
        }
        private List<UploadWorkItem> BuildUploadWorkItemsFromDataTable(DataTable dt)
        {
            var list = new List<UploadWorkItem>();
            string seasonSid = PrismAuthSessionHelper.SeasonSid;

            string? uploaderReasonSidCached = null;

            foreach (DataRow r in dt.Rows)
            {
                string dcsSid = GetDT(dt, r, "DCS");
                string dcscode = GetDT(dt, r, "DCS_CODE");

                string vendorCode = GetDT(dt, r, "VENDOR_CODE");
                string vendorSid = GetDT(dt, r, "VENDOR_SID");

                string desc1 = GetDT(dt, r, "DESC1");
                string desc2 = GetDT(dt, r, "DESC2");
                string desc3 = GetDT(dt, r, "DESC3");
                string desc4 = GetDT(dt, r, "DESC4");

                string attr = GetDT(dt, r, "ATTR");
                string size = GetDT(dt, r, "SIZE");

                string costText = GetDT(dt, r, "COST");

                string upc = GetDT(dt, r, "UPC").Trim();
                string alu = GetDT(dt, r, "ALU").Trim();

                string? upcText = UseUpcMode ? upc : null;
                string? aluText = UseUpcMode ? null : alu;

                string key = UseUpcMode
                    ? (!string.IsNullOrWhiteSpace(upcText) ? upcText : "UNKNOWN")
                    : (!string.IsNullOrWhiteSpace(aluText) ? aluText : "UNKNOWN");

                bool hasValidCost = decimal.TryParse(costText, out decimal costValue) && costValue > 0;

                string? costuploaderReasonSid = null;
                string? priceuploaderReasonSid = null;
                if (hasValidCost)
                {
                    uploaderReasonSidCached ??= ReasonRepository.GetUploaderReasonSid();
                    costuploaderReasonSid = uploaderReasonSidCached;
                    priceuploaderReasonSid = uploaderReasonSidCached;
                }
                else
                {
                    uploaderReasonSidCached ??= ReasonRepository.GetUploaderReasonSid();
                    costuploaderReasonSid = null;
                    priceuploaderReasonSid = uploaderReasonSidCached;
                }

                string sbssid = GetDT(dt, r, "SBSSID");
                string taxCodeSid = GetDT(dt, r, "TAXCODE_SID");

                var upload = new UploadData
                {
                    OriginApplication = "RProPrismWeb",

                    DefaultReasonSidForCostMemo = costuploaderReasonSid,
                    DefaultReasonSidForPriceMemo = priceuploaderReasonSid,

                    PrimaryItemDefinition = new PrimaryItemDefinition
                    {
                        dcssid = dcsSid,
                        vendsid = vendorSid,
                        description1 = desc1,
                        description2 = desc2,
                        attribute = attr == "" ? null : attr,
                        itemsize = size == "" ? null : size
                    },

                    UpdateStyleDefinition = false,
                    UpdateStyleCost = false,
                    UpdateStylePrice = false
                };

                var item = new InventoryItem
                {
                    sbssid = sbssid,
                    dcssid = dcsSid,
                    vendsid = vendorSid,
                    taxcodesid = NullIfWhiteSpace(taxCodeSid),

                    description1 = desc1 == "" ? null: desc1,
                    description2 = desc2 == "" ? null : desc2,
                    description3 = desc3 == "" ? null : desc3,
                    description4 = desc4 == "" ? null : desc4,

                    attribute = attr == "" ? null : attr,
                    itemsize = size == "" ? null : size,

                    cost = hasValidCost ? costValue : 0m,
                    lastrcvdcost = ParseDecimal(GetDT(dt, r, "ORDER_COST"), 0m),

                    spif = ParseInt(GetDT(dt, r, "SPIF"), 0),
                    serialtype = ParseInt(GetDT(dt, r, "Serial_Type"), 0),
                    lottype = ParseInt(GetDT(dt, r, "Lot_Type"), 0),

                    useqtydecimals = ParseInt(GetDT(dt, r, "Qty_Decimal"), 0),
                    regional = ParseBool(GetDT(dt, r, "Regional"), false),
                    active = true,

                    noninventory = ParseInt(GetDT(dt, r, "NON_INVENTORY"), 0),

                    upc = UseUpcMode ? upc : null,
                    alu = !UseUpcMode ? alu : null,

                    maxdiscperc1 = ParseInt(GetDT(dt, r, "MAX_DISC"), 100),
                    maxdiscperc2 = ParseInt(GetDT(dt, r, "ACC._MAX_DISC"), 100),
                    tradediscpercent = ParseInt(GetDT(dt, r, "Trade_Discount"), 0),

                    dcscode = dcscode,

                    udf1date = ParseUdfUtcDate(GetDT(dt, r, "Date_UDF_1")),
                    udf2date = ParseUdfUtcDate(GetDT(dt, r, "Date_UDF_2")),
                    udf3date = ParseUdfUtcDate(GetDT(dt, r, "Date_UDF_3")),

                    invnextend = new List<UpdateInvnExtend>(),
                    invnprice = new List<InvnPrice>()
                };

                // TEXT1–TEXT10
                for (int i = 1; i <= 10; i++)
                {
                    var val = GetDT(dt, r, $"TEXT{i}");
                    typeof(InventoryItem).GetProperty($"text{i}")?.SetValue(item, val);
                }

                // UDF handling
                UpdateInvnExtend extend = null;

                foreach (DataColumn c in dt.Columns)
                {
                    if (!c.ColumnName.StartsWith("Text_UDF", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string suffix = c.ColumnName.Substring("Text_UDF".Length);
                    if (!int.TryParse(suffix, out int idx)) continue;

                    string val = GetDT(dt, r, c.ColumnName);
                    if (string.IsNullOrWhiteSpace(val)) continue;

                    if (idx <= 5)
                    {
                        typeof(InventoryItem).GetProperty($"udf{idx}string")?.SetValue(item, val);
                    }
                    else
                    {
                        extend ??= new UpdateInvnExtend { invnsbsitemsid = null };
                        typeof(UpdateInvnExtend).GetProperty($"udf{idx}string")?.SetValue(extend, val);
                    }
                }

                // LARGE TEXT UDF
                if (extend != null)
                {
                    extend.udf1largestring = GetDT(dt, r, "Long_Text_UDF1");
                    extend.udf2largestring = GetDT(dt, r, "Long_Text_UDF2");

                    item.invnextend.Add(extend);
                }

                // PRICE LEVELS
                var addedPriceLevels = new HashSet<int>();

                foreach (DataColumn c in dt.Columns)
                {
                    if (!c.ColumnName.StartsWith("PRICE_LEVEL", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string suffix = c.ColumnName.Substring("PRICE_LEVEL".Length);
                    if (!int.TryParse(suffix, out int lvl)) continue;

                    string priceText = GetDT(dt, r, c.ColumnName);
                    if (!decimal.TryParse(priceText, out decimal price)) continue;

                    string sidCol = $"PRICE_SID{lvl}";
                    if (!dt.Columns.Contains(sidCol)) continue;

                    string priceLvlSid = GetDT(dt, r, sidCol);
                    if (string.IsNullOrWhiteSpace(priceLvlSid)) continue;

                    item.invnprice.Add(new InvnPrice
                    {
                        price = price,
                        invnsbsitemsid = null,
                        sbssid = sbssid,
                        pricelvlsid = priceLvlSid,
                        //seasonsid = seasonSid
                    });

                    if (lvl == 1)
                    {
                        item.fstprice = price;
                        item.actstrprice = price;
                        item.actstrpricewt = price;
                    }

                    addedPriceLevels.Add(lvl);
                }

                // SIMPLE MARGIN COMPUTE
                if (item.cost > 0 && item.actstrprice > 0)
                {
                    decimal margin = item.actstrprice - item.cost;

                    item.actstrmarginamt = margin;
                    item.actstrmarginamtwt = margin;
                    item.actstrmarginpctg = (margin / item.actstrprice) * 100;
                    item.actstrmarkuppctg = (margin / item.cost) * 100;
                    item.actstrcoefficient = item.actstrprice / item.cost;
                }

                item.activestoresid = NullIfWhiteSpace(GetDT(dt, r, "ACTIVESTORESID"));
                item.activepricelevelsid = NullIfWhiteSpace(GetDT(dt, r, "ACTIVEPRICELEVELSID"));
                item.activeseasonsid = NullIfWhiteSpace(GetDT(dt, r, "ACTIVESEASONSID")) ?? seasonSid;

                upload.InventoryItems.Add(item);

                list.Add(new UploadWorkItem
                {
                    Key = key,
                    Data = upload
                });
            }

            return list;
        }

        private static string GetDT(DataTable dt, DataRow r, string col)
        {
            if (!dt.Columns.Contains(col)) return "";
            return r[col]?.ToString() ?? "";
        }

        private void MakeGridReadable()
        {
            if (dgvInventory.Columns.Count == 0) return;

            dgvInventory.SuspendLayout();

            dgvInventory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvInventory.ScrollBars = ScrollBars.Both;
            dgvInventory.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgvInventory.RowTemplate.Height = 32;

            dgvInventory.AllowUserToResizeRows = false;
            dgvInventory.AllowUserToResizeColumns = true;

            dgvInventory.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvInventory.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvInventory.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            int fixedWidth = 160;
            foreach (DataGridViewColumn col in dgvInventory.Columns)
            {
                col.Width = fixedWidth;
                col.MinimumWidth = 120;
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            dgvInventory.ResumeLayout();
        }

        private void HideEmptyColumns()
        {
            int hidden = 0;

            foreach (DataGridViewColumn col in dgvInventory.Columns)
            {
                bool hasData = false;

                foreach (DataGridViewRow row in dgvInventory.Rows)
                {
                    if (row.IsNewRow) continue;

                    var cellValue = row.Cells[col.Index].Value;
                    if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                    {
                        hasData = true;
                        break;
                    }
                }

                col.Visible = hasData;
                if (!hasData) hidden++;
            }

            LogInfo($"Hidden empty columns: {hidden}");
        }

        private void ApplyTableBorder()
        {
            dgvInventory.BorderStyle = BorderStyle.FixedSingle;
            dgvInventory.GridColor = Color.FromArgb(200, 200, 200);

            dgvInventory.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgvInventory.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgvInventory.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            dgvInventory.EnableHeadersVisualStyles = false;

            dgvInventory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 248);
            dgvInventory.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgvInventory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            dgvInventory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 252);
            dgvInventory.DefaultCellStyle.SelectionForeColor = Color.Black;
        }

        private static int ParseInt(string value, int fallback) => int.TryParse(value, out int v) ? v : fallback;

        private static decimal ParseDecimal(string value, decimal fallback) => decimal.TryParse(value, out decimal v) ? v : fallback;

        private static bool ParseBool(string value, bool fallback)
        {
            if (bool.TryParse(value, out bool b)) return b;
            if (int.TryParse(value, out int i)) return i != 0;

            string t = (value ?? "").Trim().ToUpperInvariant();
            if (t == "Y" || t == "YES" || t == "TRUE") return true;
            if (t == "N" || t == "NO" || t == "FALSE") return false;

            return fallback;
        }

        private void LogInfo(string message) => AppendLog("INFO", message);
        private void LogWarn(string message) => AppendLog("WARN", message);
        private void LogError(string message) => AppendLog("ERROR", message);

        private void AppendLog(string level, string message)
        {
            if (txtLogs == null || txtLogs.IsDisposed) return;

            void Write()
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

                txtLogs.AppendText(line + Environment.NewLine);
                txtLogs.SelectionStart = txtLogs.Text.Length;
                txtLogs.ScrollToCaret();

                try
                {
                    string path = GetDailyFilePath("Logs", "InventoryLog", "txt");
                    AppendLineToFile(path, line);
                }
                catch
                {
                }
            }

            if (txtLogs.InvokeRequired)
                txtLogs.BeginInvoke((Action)Write);
            else
                Write();
        }

        private string BuildVendorPayload(string vendCode, string vendName, string subsidiarySid)
        {
            var payload = new
            {
                data = new[]
                {
                    new
                    {
                        originapplication = "RProPrismWeb",
                        active = true,
                        vendorterm = Array.Empty<object>(),
                        vendoraddress = Array.Empty<object>(),
                        vendorcontact = Array.Empty<object>(),
                        sbssid = subsidiarySid,
                        regional = false,
                        vendcode = vendCode,
                        vendname = vendName
                    }
                }
            };

            return JsonSerializer.Serialize(payload);
        }

        private async Task CreateVendorAsync(string vendCode, string vendName, string subsidiarySid)
        {
            if (string.IsNullOrWhiteSpace(subsidiarySid))
                throw new Exception("Subsidiary SID is empty. Unable to create vendor.");

            try
            {
                string url = BuildVendorUrl();

                string auth = await GetAuthSessionAsync().ConfigureAwait(false);

                string json = BuildVendorPayload(vendCode, vendName, subsidiarySid);

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                req.Headers.TryAddWithoutValidation("auth-session", auth);
                req.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, version=2");

                using var resp = await _http.SendAsync(req).ConfigureAwait(false);
                string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (IsAuthExpiredStatus(resp))
                {
                    PrismAuthSessionHelper.ClearCache();

                    string auth2 = await GetAuthSessionAsync(forceRefresh: true).ConfigureAwait(false);

                    string retryJson = BuildVendorPayload(vendCode, vendName, subsidiarySid);

                    using var retryReq = new HttpRequestMessage(HttpMethod.Post, url);
                    retryReq.Content = new StringContent(retryJson, Encoding.UTF8, "application/json");
                    retryReq.Headers.TryAddWithoutValidation("auth-session", auth2);
                    retryReq.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, version=2");

                    using var retryResp = await _http.SendAsync(retryReq).ConfigureAwait(false);
                    string retryBody = await retryResp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    LogInfo($"Vendor create API HTTP {(int)retryResp.StatusCode}");

                    if (!retryResp.IsSuccessStatusCode)
                        LogError(retryBody);
                    else
                        LogInfo($"Vendor created: {vendCode} | SBS_SID: {subsidiarySid}");

                    return;
                }

                LogInfo($"Vendor create API HTTP {(int)resp.StatusCode}");

                if (!resp.IsSuccessStatusCode)
                    LogError(body);
                else
                    LogInfo($"Vendor created: {vendCode} | SBS_SID: {subsidiarySid}");
            }
            catch (Exception ex)
            {
                LogError($"CreateVendorAsync failed: {ex.Message}");
            }
        }


        //private void UpdateTextUdfSidFromDb(DataTable payloadDt)         //old adding new data if not existed
        //{
        //    if (payloadDt.Rows.Count == 0)
        //    {
        //        LogWarn("[TEXT_UDF_SID] Payload DataTable is empty.");
        //        return;
        //    }

        //    using var conn = OpenOracleConnection();

        //    var udfIndexes = new List<int>();

        //    foreach (DataColumn col in payloadDt.Columns)
        //    {
        //        string name = (col.ColumnName ?? "").Trim();

        //        if (!name.StartsWith("TEXT_UDF", StringComparison.OrdinalIgnoreCase))
        //            continue;

        //        string suffix = name.Substring("TEXT_UDF".Length);
        //        if (!int.TryParse(suffix, out int idx))
        //            continue;

        //        udfIndexes.Add(idx);
        //    }

        //    udfIndexes = udfIndexes.Distinct().OrderBy(x => x).ToList();

        //    if (udfIndexes.Count == 0)
        //    {
        //        //LogWarn("[TEXT_UDF_SID] No TEXT_UDF{n} columns found.");
        //        return;
        //    }

        //    foreach (int idx in udfIndexes)
        //    {
        //        string sidCol = $"TEXT_UDF_SID{idx}";
        //        if (!payloadDt.Columns.Contains(sidCol))
        //            payloadDt.Columns.Add(sidCol, typeof(string));
        //    }

        //    for (int r = 0; r < payloadDt.Rows.Count; r++)
        //    {
        //        foreach (int idx in udfIndexes)
        //        {
        //            string udfCol = $"TEXT_UDF{idx}";
        //            string sidCol = $"TEXT_UDF_SID{idx}";

        //            string udfOption = payloadDt.Rows[r][udfCol]?.ToString()?.Trim() ?? "";

        //            if (string.IsNullOrWhiteSpace(udfOption))
        //            {
        //                payloadDt.Rows[r][sidCol] = DBNull.Value;
        //                continue;
        //            }

        //            string udfSid = "";
        //            using (var cmdUdf = new OracleCommand(
        //                @"SELECT sid
        //                  FROM rps.invn_udf
        //                  WHERE udf_no = :udfNo
        //                  FETCH FIRST 1 ROWS ONLY",
        //                conn))
        //            {
        //                cmdUdf.BindByName = true;
        //                cmdUdf.CommandTimeout = OracleCommandTimeoutSeconds;
        //                cmdUdf.Parameters.Add(":udfNo", OracleDbType.Int32).Value = idx;
        //                udfSid = cmdUdf.ExecuteScalar()?.ToString()?.Trim() ?? "";
        //            }

        //            if (string.IsNullOrWhiteSpace(udfSid))
        //            {
        //                payloadDt.Rows[r][sidCol] = DBNull.Value;
        //                //LogWarn($"[TEXT_UDF_SID][Row {r + 1}] No UDF SID found for TEXT_UDF{idx}.");
        //                continue;
        //            }

        //            string optionSid = "";
        //            using (var cmdOpt = new OracleCommand(
        //                @"SELECT sid
        //                  FROM rps.invn_udf_option
        //                  WHERE udf_sid = :udfSid
        //                    AND udf_option = :udfOption
        //                  FETCH FIRST 1 ROWS ONLY",
        //                conn))
        //            {
        //                cmdOpt.BindByName = true;
        //                cmdOpt.CommandTimeout = OracleCommandTimeoutSeconds;
        //                cmdOpt.Parameters.Add(":udfSid", OracleDbType.Varchar2).Value = udfSid;
        //                cmdOpt.Parameters.Add(":udfOption", OracleDbType.Varchar2).Value = udfOption;

        //                optionSid = cmdOpt.ExecuteScalar()?.ToString()?.Trim() ?? "";
        //            }

        //            if (string.IsNullOrWhiteSpace(optionSid))
        //            {
        //                CreateInvnUdfOptionAsync(udfSid, udfOption).GetAwaiter().GetResult();

        //                using var cmdOpt2 = new OracleCommand(
        //                    @"SELECT sid
        //                      FROM rps.invn_udf_option
        //                      WHERE udf_sid = :udfSid
        //                        AND udf_option = :udfOption
        //                      FETCH FIRST 1 ROWS ONLY",
        //                    conn);

        //                cmdOpt2.BindByName = true;
        //                cmdOpt2.CommandTimeout = OracleCommandTimeoutSeconds;
        //                cmdOpt2.Parameters.Add(":udfSid", OracleDbType.Varchar2).Value = udfSid;
        //                cmdOpt2.Parameters.Add(":udfOption", OracleDbType.Varchar2).Value = udfOption;

        //                optionSid = cmdOpt2.ExecuteScalar()?.ToString()?.Trim() ?? "";
        //            }

        //            if (!string.IsNullOrWhiteSpace(optionSid))
        //            {
        //                payloadDt.Rows[r][sidCol] = optionSid;
        //            }
        //            else
        //            {
        //                payloadDt.Rows[r][sidCol] = DBNull.Value;
        //                //LogWarn($"[TEXT_UDF_SID][Row {r + 1}] Failed to get/create SID for {udfCol} value '{udfOption}'.");
        //            }
        //        }
        //    }

        //    LogInfo($"[TEXT_UDF_SID] Done. Generated TEXT_UDF_SID columns for indexes: {string.Join(",", udfIndexes)}");
        //}

        private void UpdateTextUdfSidFromDb(DataTable payloadDt)
        {
            if (payloadDt.Rows.Count == 0)
            {
                LogWarn("[TEXT_UDF_SID] Payload DataTable is empty.");
                return;
            }

            using var conn = OpenOracleConnection();

            var udfIndexes = new List<int>();

            foreach (DataColumn col in payloadDt.Columns)
            {
                string name = (col.ColumnName ?? "").Trim();

                if (!name.StartsWith("TEXT_UDF", StringComparison.OrdinalIgnoreCase))
                    continue;

                string suffix = name.Substring("TEXT_UDF".Length);
                if (!int.TryParse(suffix, out int idx))
                    continue;

                udfIndexes.Add(idx);
            }

            udfIndexes = udfIndexes.Distinct().OrderBy(x => x).ToList();

            if (udfIndexes.Count == 0)
            {
                return;
            }

            // Ensure SID columns exist
            foreach (int idx in udfIndexes)
            {
                string sidCol = $"TEXT_UDF_SID{idx}";
                if (!payloadDt.Columns.Contains(sidCol))
                    payloadDt.Columns.Add(sidCol, typeof(string));
            }

            for (int r = 0; r < payloadDt.Rows.Count; r++)
            {
                foreach (int idx in udfIndexes)
                {
                    string udfCol = $"TEXT_UDF{idx}";
                    string sidCol = $"TEXT_UDF_SID{idx}";

                    string udfOption = payloadDt.Rows[r][udfCol]?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(udfOption))
                    {
                        payloadDt.Rows[r][sidCol] = DBNull.Value;
                        continue;
                    }

                    // Get UDF SID
                    string udfSid = "";
                    using (var cmdUdf = new OracleCommand(
                        @"SELECT sid
                  FROM rps.invn_udf
                  WHERE udf_no = :udfNo
                  FETCH FIRST 1 ROWS ONLY",
                        conn))
                    {
                        cmdUdf.BindByName = true;
                        cmdUdf.CommandTimeout = OracleCommandTimeoutSeconds;
                        cmdUdf.Parameters.Add(":udfNo", OracleDbType.Int32).Value = idx;

                        udfSid = cmdUdf.ExecuteScalar()?.ToString()?.Trim() ?? "";
                    }

                    if (string.IsNullOrWhiteSpace(udfSid))
                    {
                        payloadDt.Rows[r][sidCol] = DBNull.Value;
                        continue;
                    }

                    // Get Option SID (NO CREATE)
                    string optionSid = "";
                    using (var cmdOpt = new OracleCommand(
                        @"SELECT sid
                  FROM rps.invn_udf_option
                  WHERE udf_sid = :udfSid
                    AND udf_option = :udfOption
                  FETCH FIRST 1 ROWS ONLY",
                        conn))
                    {
                        cmdOpt.BindByName = true;
                        cmdOpt.CommandTimeout = OracleCommandTimeoutSeconds;
                        cmdOpt.Parameters.Add(":udfSid", OracleDbType.Varchar2).Value = udfSid;
                        cmdOpt.Parameters.Add(":udfOption", OracleDbType.Varchar2).Value = udfOption;

                        optionSid = cmdOpt.ExecuteScalar()?.ToString()?.Trim() ?? "";
                    }

                    if (!string.IsNullOrWhiteSpace(optionSid))
                    {
                        payloadDt.Rows[r][sidCol] = optionSid;
                    }
                    else
                    {
                        // Do NOT create if not exists
                        payloadDt.Rows[r][sidCol] = DBNull.Value;

                        // Optional logging
                        //LogWarn($"[TEXT_UDF_SID][Row {r + 1}] Option not found for {udfCol} value '{udfOption}'.");
                    }
                }
            }

            LogInfo($"[TEXT_UDF_SID] Done. Generated TEXT_UDF_SID columns for indexes: {string.Join(",", udfIndexes)}");
        }

        private async Task CreateInvnUdfOptionAsync(string udfSid, string udfOption)
        {
            try
            {
                string url = BuildUDFUrl();

                string json = JsonSerializer.Serialize(new
                {
                    data = new[]
                    {
                        new
                        {
                            originapplication = "RProPrismWeb",
                            udfsid = udfSid,
                            udfoption = udfOption,
                            active = true
                        }
                    }
                });

                string auth = await GetAuthSessionAsync().ConfigureAwait(false);

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                req.Headers.TryAddWithoutValidation("auth-session", auth);
                req.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, version=2");

                using var resp = await _http.SendAsync(req).ConfigureAwait(false);
                string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (IsAuthExpiredStatus(resp))
                {
                    PrismAuthSessionHelper.ClearCache();
                    string auth2 = await GetAuthSessionAsync(forceRefresh: true).ConfigureAwait(false);

                    using var retryReq = new HttpRequestMessage(HttpMethod.Post, url);
                    retryReq.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    retryReq.Headers.TryAddWithoutValidation("auth-session", auth2);
                    retryReq.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, version=2");

                    using var retryResp = await _http.SendAsync(retryReq).ConfigureAwait(false);
                    string retryBody = await retryResp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    LogInfo($"UDF create API HTTP {(int)retryResp.StatusCode}");
                    if (!retryResp.IsSuccessStatusCode)
                    {
                        WriteApiPayloadResponseToDailyFile(udfSid, json, body);
                        LogError(retryBody);
                    }
                    else
                        WriteApiPayloadResponseToDailyFile(udfSid, json, body);
                    LogInfo($"UDF created: udfSid={udfSid}, udfOption={udfOption}");

                    return;
                }

                LogInfo($"UDF create API HTTP {(int)resp.StatusCode}");

                if (!resp.IsSuccessStatusCode)
                    LogError(body);
                else
                    LogInfo($"UDF created: udfSid={udfSid}, udfOption={udfOption}");
            }
            catch (Exception ex)
            {
                LogError($"CreateInvnUdfOptionAsync failed: {ex.Message}");
            }
        }

        private string GetItemCodeFromWorkItem(UploadWorkItem work)
        {
            if (work == null)
                return string.Empty;
            if (!string.IsNullOrWhiteSpace(work.Key))
                return work.Key.Trim();
            return string.Empty;
        }

        private ExistingPriceInfo? GetExistingPriceByLevel(ExistingItemInfo existing, int level)
        {
            if (existing?.Prices == null || existing.Prices.Count == 0)
                return null;

            return existing.Prices
                .FirstOrDefault(x => x.price_lvl.HasValue && x.price_lvl.Value == level);
        }

        private string? DbToString(IDataRecord reader, string col)
        {
            int i = reader.GetOrdinal(col);
            return reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i))?.Trim();
        }

        private decimal? DbToDecimal(IDataRecord reader, string col)
        {
            int i = reader.GetOrdinal(col);
            if (reader.IsDBNull(i)) return null;
            return Convert.ToDecimal(reader.GetValue(i));
        }

        private string FormatCsvDate(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            // Handles timestamp with timezone
            if (DateTimeOffset.TryParse(value.ToString(), out var dto))
                return dto.ToString("MM/dd/yyyy");

            // fallback if already DateTime
            if (DateTime.TryParse(value.ToString(), out var dt))
                return dt.ToString("MM/dd/yyyy");

            return null;
        }

        private async Task<ExistingItemInfo?> GetExistingItemInfoAsync(string itemCode, bool useUpcMode, string sbsNo)
        {
            string whereClause = useUpcMode ? "i.upc = :itemCode" : "i.alu = :itemCode";

            string sql = $@"
            SELECT
                i.sid,
                i.alu,
                v.vend_name AS vendor_name,
                v.vend_code AS vendor_code,
                i.udf1_date,
                i.udf2_date,
                i.udf3_date,
                i.upc,
                d.dcs_code,
                i.description1,
                i.description2,
                i.description3,
                i.description4,
                i.attribute,
                i.cost,
                i.serial_type AS serialtype,
                i.lot_type AS lottype,
                i.non_inventory AS noninventory,

                -- UDF 1–5
                i.udf1_string,
                i.udf2_string,
                i.udf3_string,
                i.udf4_string,
                i.udf5_string,

                -- UDF 6–15
                ie.sid AS extend_sid,
                ie.udf6_string,
                ie.udf7_string,
                ie.udf8_string,
                ie.udf9_string,
                ie.udf10_string,
                ie.udf11_string,
                ie.udf12_string,
                ie.udf13_string,
                ie.udf14_string,
                ie.udf15_string,

                i.spif,

                -- TEXT 1–10
                i.text1,
                i.text2,
                i.text3,
                i.text4,
                i.text5,
                i.text6,
                i.text7,
                i.text8,
                i.text9,
                i.text10,
                t.sid AS taxcodesid,

                s.sbs_no,

                -- PRICE LEVEL 1
                pl1.sid AS price_sid_1,
                pl1.price_lvl_sid AS price_lvl_sid_1,
                pl1.price AS price_lvl_1,

                -- PRICE LEVEL 2
                pl2.sid AS price_sid_2,
                pl2.price_lvl_sid AS price_lvl_sid_2,
                pl2.price AS price_lvl_2,

                -- PRICE LEVEL 3
                pl3.sid AS price_sid_3,
                pl3.price_lvl_sid AS price_lvl_sid_3,
                pl3.price AS price_lvl_3,

                -- PRICE LEVEL 4
                pl4.sid AS price_sid_4,
                pl4.price_lvl_sid AS price_lvl_sid_4,
                pl4.price AS price_lvl_4,

                -- PRICE LEVEL 5
                pl5.sid AS price_sid_5,
                pl5.price_lvl_sid AS price_lvl_sid_5,
                pl5.price AS price_lvl_5

            FROM RPS.SUBSIDIARY s

            LEFT JOIN RPS.INVN_SBS_ITEM i
                ON i.SBS_SID = s.SID

            LEFT JOIN RPS.DCS d
                ON d.SID = i.dcs_sid

            LEFT JOIN RPS.INVN_SBS_EXTEND ie
                ON ie.INVN_SBS_ITEM_SID = i.SID

            LEFT JOIN RPS.TAX_CODE t
                ON t.SID = i.tax_code_sid

            LEFT JOIN RPS.VENDOR v
                ON v.sid = i.vend_sid

            -- LEVEL 1
            LEFT JOIN (
                SELECT 
                    p.sid,
                    p.INVN_SBS_ITEM_SID, 
                    p.price,
                    p.PRICE_LVL_SID,
                    pl.SBS_SID
                FROM RPS.INVN_SBS_PRICE p
                INNER JOIN RPS.PRICE_LEVEL pl
                    ON pl.SID = p.PRICE_LVL_SID
                WHERE pl.price_lvl = 1
            ) pl1
                ON pl1.INVN_SBS_ITEM_SID = i.SID
               AND pl1.SBS_SID = s.SID

            -- LEVEL 2
            LEFT JOIN (
                SELECT 
                    p.sid,
                    p.INVN_SBS_ITEM_SID, 
                    p.price,
                    p.PRICE_LVL_SID,
                    pl.SBS_SID
                FROM RPS.INVN_SBS_PRICE p
                INNER JOIN RPS.PRICE_LEVEL pl
                    ON pl.SID = p.PRICE_LVL_SID
                WHERE pl.price_lvl = 2
            ) pl2
                ON pl2.INVN_SBS_ITEM_SID = i.SID
               AND pl2.SBS_SID = s.SID

            -- LEVEL 3
            LEFT JOIN (
                SELECT 
                    p.sid,
                    p.INVN_SBS_ITEM_SID, 
                    p.price,
                    p.PRICE_LVL_SID,
                    pl.SBS_SID
                FROM RPS.INVN_SBS_PRICE p
                INNER JOIN RPS.PRICE_LEVEL pl
                    ON pl.SID = p.PRICE_LVL_SID
                WHERE pl.price_lvl = 3
            ) pl3
                ON pl3.INVN_SBS_ITEM_SID = i.SID
               AND pl3.SBS_SID = s.SID

            -- LEVEL 4
            LEFT JOIN (
                SELECT 
                    p.sid,
                    p.INVN_SBS_ITEM_SID, 
                    p.price,
                    p.PRICE_LVL_SID,
                    pl.SBS_SID
                FROM RPS.INVN_SBS_PRICE p
                INNER JOIN RPS.PRICE_LEVEL pl
                    ON pl.SID = p.PRICE_LVL_SID
                WHERE pl.price_lvl = 4
            ) pl4
                ON pl4.INVN_SBS_ITEM_SID = i.SID
               AND pl4.SBS_SID = s.SID

            -- LEVEL 5
            LEFT JOIN (
                SELECT 
                    p.sid,
                    p.INVN_SBS_ITEM_SID, 
                    p.price,
                    p.PRICE_LVL_SID,
                    pl.SBS_SID
                FROM RPS.INVN_SBS_PRICE p
                INNER JOIN RPS.PRICE_LEVEL pl
                    ON pl.SID = p.PRICE_LVL_SID
                WHERE pl.price_lvl = 5
            ) pl5
                ON pl5.INVN_SBS_ITEM_SID = i.SID
               AND pl5.SBS_SID = s.SID

            WHERE {whereClause}
              AND s.sid = :sid";

            using var conn = OpenOracleConnection();
            using var cmd = new OracleCommand(sql, conn);

            cmd.BindByName = true;
            cmd.CommandTimeout = OracleCommandTimeoutSeconds;

            cmd.Parameters.Add("itemCode", OracleDbType.Varchar2).Value = itemCode.Trim();
            cmd.Parameters.Add("sid", OracleDbType.Varchar2).Value = sbsNo;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var info = new ExistingItemInfo
            {
                sid = DbToString(reader, "sid"),
                alu = DbToString(reader, "alu"),
                upc = DbToString(reader, "upc"),
                vendor_name = DbToString(reader, "vendor_name"),
                vendor_code = DbToString(reader, "vendor_code"),
                lottype = Convert.ToInt32(reader["lottype"]),
                serialtype = Convert.ToInt32(reader["serialtype"]),
                noninventory = Convert.ToInt32(reader["noninventory"]),
                udf1date = DbToString(reader, "udf1_date"),
                udf2date = DbToString(reader, "udf2_date"),
                udf3date = DbToString(reader, "udf3_date"),
                dcs_code = DbToString(reader, "dcs_code"),

                description1 = DbToString(reader, "description1"),
                description2 = DbToString(reader, "description2"),
                description3 = DbToString(reader, "description3"),
                description4 = DbToString(reader, "description4"),

                attribute = DbToString(reader, "attribute"),
                cost = DbToDecimal(reader, "cost"),

                taxcodesid = DbToString(reader, "taxcodesid"),
                //row_version = DbToString(reader, "row_version"),

                // ✅ UDF 1–5
                udf1_string = DbToString(reader, "udf1_string"),
                udf2_string = DbToString(reader, "udf2_string"),
                udf3_string = DbToString(reader, "udf3_string"),
                udf4_string = DbToString(reader, "udf4_string"),
                udf5_string = DbToString(reader, "udf5_string"),

                // ✅ UDF 6–15
                udf6_string = DbToString(reader, "udf6_string"),
                udf7_string = DbToString(reader, "udf7_string"),
                udf8_string = DbToString(reader, "udf8_string"),
                udf9_string = DbToString(reader, "udf9_string"),
                udf10_string = DbToString(reader, "udf10_string"),
                udf11_string = DbToString(reader, "udf11_string"),
                udf12_string = DbToString(reader, "udf12_string"),
                udf13_string = DbToString(reader, "udf13_string"),
                udf14_string = DbToString(reader, "udf14_string"),
                udf15_string = DbToString(reader, "udf15_string"),

                // ✅ EXTEND SID
                extend_sid = DbToString(reader, "extend_sid"),

                // ✅ TEXT 1–10
                text1 = DbToString(reader, "text1"),
                text2 = DbToString(reader, "text2"),
                text3 = DbToString(reader, "text3"),
                text4 = DbToString(reader, "text4"),
                text5 = DbToString(reader, "text5"),
                text6 = DbToString(reader, "text6"),
                text7 = DbToString(reader, "text7"),
                text8 = DbToString(reader, "text8"),
                text9 = DbToString(reader, "text9"),
                text10 = DbToString(reader, "text10"),

                spif = DbToDecimal(reader, "spif"),

                Prices = new List<ExistingPriceInfo>
                {
                    new ExistingPriceInfo
                    {
                        sid = DbToString(reader, "price_sid_1"),
                        price_lvl = 1,
                        price = DbToDecimal(reader, "price_lvl_1"),
                        price_lvl_sid = DbToString(reader, "price_lvl_sid_1")
                    },
                    new ExistingPriceInfo
                    {
                        sid = DbToString(reader, "price_sid_2"),
                        price_lvl = 2,
                        price = DbToDecimal(reader, "price_lvl_2"),
                        price_lvl_sid = DbToString(reader, "price_lvl_sid_2")
                    },
                    new ExistingPriceInfo
                    {
                        sid = DbToString(reader, "price_sid_3"),
                        price_lvl = 3,
                        price = DbToDecimal(reader, "price_lvl_3"),
                        price_lvl_sid = DbToString(reader, "price_lvl_sid_3")
                    },
                    new ExistingPriceInfo
                    {
                        sid = DbToString(reader, "price_sid_4"),
                        price_lvl = 4,
                        price = DbToDecimal(reader, "price_lvl_4"),
                        price_lvl_sid = DbToString(reader, "price_lvl_sid_4")
                    },
                    new ExistingPriceInfo
                    {
                        sid = DbToString(reader, "price_sid_5"),
                        price_lvl = 5,
                        price = DbToDecimal(reader, "price_lvl_5"),
                        price_lvl_sid = DbToString(reader, "price_lvl_sid_5")
                    }
                }
            };

            return info;
        }

        private UpdateUploadRoot BuildUpdatePayload(UploadWorkItem work, ExistingItemInfo existing)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            if (work.Data == null)
                throw new ArgumentNullException(nameof(work.Data));

            if (existing == null)
                throw new ArgumentNullException(nameof(existing));

            var sourcePrimary = work.Data.PrimaryItemDefinition
                ?? throw new Exception("PrimaryItemDefinition is missing.");

            var sourceInv = work.Data.InventoryItems?.FirstOrDefault()
                ?? throw new Exception("InventoryItems is missing.");

            var seasonSid = string.IsNullOrWhiteSpace(sourceInv.activeseasonsid)
                ? PrismAuthSessionHelper.SeasonSid
                : sourceInv.activeseasonsid;

            var itemSid = NullIfWhiteSpace(existing.sid) ?? "";

            //LogInfo($"[UPDATE][{work.Key}] SBSSID={sourceInv.sbssid}, TAXCODE={sourceInv.taxcodesid}, DCSCODE={sourceInv.dcscode}");

            //var updateItem = new UpdateInventoryItem
            //{
            //    sid = itemSid,
            //    sbssid = NullIfWhiteSpace(sourceInv.sbssid) ?? "",
            //    dcscode = sourceInv.dcscode,
            //    dcssid = sourceInv.dcssid,
            //    description1 = EmptyIfNull(sourcePrimary.description1),
            //    description2 = EmptyIfNull(sourcePrimary.description2),
            //    description3 = EmptyIfNull(sourceInv.description3),
            //    description4 = EmptyIfNull(sourceInv.description4),

            //    taxcodesid = EmptyIfNull(sourceInv.taxcodesid),
            //    cost = sourceInv.cost > 0 ? sourceInv.cost : 0,

            //    text1 = EmptyIfNull(sourceInv.text1),
            //    text2 = EmptyIfNull(sourceInv.text2),
            //    text3 = EmptyIfNull(sourceInv.text3),
            //    text4 = EmptyIfNull(sourceInv.text4),
            //    text5 = EmptyIfNull(sourceInv.text5),
            //    text6 = EmptyIfNull(sourceInv.text6),
            //    text7 = EmptyIfNull(sourceInv.text7),
            //    text8 = EmptyIfNull(sourceInv.text8),
            //    text9 = EmptyIfNull(sourceInv.text9),
            //    text10 = EmptyIfNull(sourceInv.text10),

            //    attribute = EmptyIfNull(sourcePrimary.attribute),
            //    itemsize = EmptyIfNull(sourcePrimary.itemsize),

            //    lottype = sourceInv.lottype,
            //    serialtype = sourceInv.serialtype,
            //    noninventory = sourceInv.noninventory,

            //    udf1string = EmptyIfNull(sourceInv.udf1string),
            //    udf2string = EmptyIfNull(sourceInv.udf2string),
            //    udf3string = EmptyIfNull(sourceInv.udf3string),
            //    udf4string = EmptyIfNull(sourceInv.udf4string),
            //    udf5string = EmptyIfNull(sourceInv.udf5string),

            //    upc = UseUpcMode ? NullIfWhiteSpace(sourceInv.upc) : null,
            //    alu = UseUpcMode ? null : NullIfWhiteSpace(sourceInv.alu),

            //    kittype = sourceInv.kittype,
            //    ltypriceinpoints = sourceInv.ltypriceinpoints,
            //    ltypointsearned = sourceInv.ltypointsearned,

            //    activestoresid = NullIfWhiteSpace(sourceInv.activestoresid),
            //    activepricelevelsid = NullIfWhiteSpace(sourceInv.activepricelevelsid),
            //    activeseasonsid = seasonSid,

            //    actstrpricewt = sourceInv.actstrpricewt,
            //    spif = sourceInv.spif,

            //    udf1date = sourceInv.udf1date,
            //    udf2date = sourceInv.udf2date,
            //    udf3date = sourceInv.udf3date,

            //    invnprice = new List<UpdateInvnPrice>(),
            //    invnextend = new List<UpdateInvnExtend>()
            //};

            var updateItem = new UpdateInventoryItem
            {
                sid = itemSid,
                sbssid = NullIfWhiteSpace(sourceInv.sbssid),
                dcscode = NullIfWhiteSpace(sourceInv.dcscode),
                dcssid = NullIfWhiteSpace(sourceInv.dcssid),

                description1 = NullIfWhiteSpace(sourcePrimary.description1),
                description2 = NullIfWhiteSpace(sourcePrimary.description2),
                description3 = NullIfWhiteSpace(sourceInv.description3),
                description4 = NullIfWhiteSpace(sourceInv.description4),

                taxcodesid = NullIfWhiteSpace(sourceInv.taxcodesid),

                // if cost <= 0 → null (make sure cost is nullable: decimal?)
                cost = sourceInv.cost > 0 ? sourceInv.cost : null,

                text1 = NullIfWhiteSpace(sourceInv.text1),
                text2 = NullIfWhiteSpace(sourceInv.text2),
                text3 = NullIfWhiteSpace(sourceInv.text3),
                text4 = NullIfWhiteSpace(sourceInv.text4),
                text5 = NullIfWhiteSpace(sourceInv.text5),
                text6 = NullIfWhiteSpace(sourceInv.text6),
                text7 = NullIfWhiteSpace(sourceInv.text7),
                text8 = NullIfWhiteSpace(sourceInv.text8),
                text9 = NullIfWhiteSpace(sourceInv.text9),
                text10 = NullIfWhiteSpace(sourceInv.text10),

                attribute = NullIfWhiteSpace(sourcePrimary.attribute),
                itemsize = NullIfWhiteSpace(sourcePrimary.itemsize),

                lottype = sourceInv.lottype,
                serialtype = sourceInv.serialtype,
                noninventory = sourceInv.noninventory,

                udf1string = NullIfWhiteSpace(sourceInv.udf1string),
                udf2string = NullIfWhiteSpace(sourceInv.udf2string),
                udf3string = NullIfWhiteSpace(sourceInv.udf3string),
                udf4string = NullIfWhiteSpace(sourceInv.udf4string),
                udf5string = NullIfWhiteSpace(sourceInv.udf5string),

                upc = UseUpcMode ? NullIfWhiteSpace(sourceInv.upc) : null,
                alu = UseUpcMode ? null : NullIfWhiteSpace(sourceInv.alu),

                kittype = sourceInv.kittype,
                ltypriceinpoints = sourceInv.ltypriceinpoints,
                ltypointsearned = sourceInv.ltypointsearned,

                activestoresid = NullIfWhiteSpace(sourceInv.activestoresid),
                activepricelevelsid = NullIfWhiteSpace(sourceInv.activepricelevelsid),
                activeseasonsid = seasonSid,

                actstrpricewt = sourceInv.actstrpricewt,
                spif = sourceInv.spif,

                udf1date = sourceInv.udf1date,
                udf2date = sourceInv.udf2date,
                udf3date = sourceInv.udf3date,

                invnprice = new List<UpdateInvnPrice>(),
                invnextend = new List<UpdateInvnExtend>()
            };

            // =========================================================
            // ✅ FIXED INVNEXTEND HANDLING
            // =========================================================
            var sourceExtend = sourceInv.invnextend?.FirstOrDefault();
            string? extendSid = GetExtendSid(itemSid);
            bool hasExtendData =
                sourceExtend != null &&
                (
                    !string.IsNullOrWhiteSpace(sourceExtend.udf6string) ||
                    !string.IsNullOrWhiteSpace(sourceExtend.udf7string) ||
                    !string.IsNullOrWhiteSpace(sourceExtend.udf8string) ||
                    !string.IsNullOrWhiteSpace(sourceExtend.udf9string) ||
                    !string.IsNullOrWhiteSpace(sourceExtend.udf10string) ||
                    !string.IsNullOrWhiteSpace(sourceExtend.udf11string) ||
                    !string.IsNullOrWhiteSpace(sourceExtend.udf12string) ||
                    !string.IsNullOrWhiteSpace(sourceExtend.udf13string) ||
                    !string.IsNullOrWhiteSpace(sourceExtend.udf14string) ||
                    !string.IsNullOrWhiteSpace(sourceExtend.udf15string)
                );

            if (hasExtendData)
            {
                updateItem.invnextend.Add(new UpdateInvnExtend
                {
                    sid = extendSid, // null = insert, else update
                    invnsbsitemsid = itemSid,
                    udf6string = EmptyIfNull(sourceExtend?.udf6string),
                    udf7string = EmptyIfNull(sourceExtend?.udf7string),
                    udf8string = EmptyIfNull(sourceExtend?.udf8string),
                    udf9string = EmptyIfNull(sourceExtend?.udf9string),
                    udf10string = EmptyIfNull(sourceExtend?.udf10string),
                    udf11string = EmptyIfNull(sourceExtend?.udf11string),
                    udf12string = EmptyIfNull(sourceExtend?.udf12string),
                    udf13string = EmptyIfNull(sourceExtend?.udf13string),
                    udf14string = EmptyIfNull(sourceExtend?.udf14string),
                    udf15string = EmptyIfNull(sourceExtend?.udf15string)
                });

                //LogInfo($"[UPDATE][{work.Key}] Added invnextend with UDF6–15");
            }
            else
            {
                //LogInfo($"[UPDATE][{work.Key}] Skipping invnextend (no data)");
            }

            // =========================================================
            // PRICE HANDLING
            // =========================================================
            if (sourceInv.invnprice != null && sourceInv.invnprice.Count > 0)
            {
                foreach (var srcPrice in sourceInv.invnprice)
                {
                    if (srcPrice == null || srcPrice.price == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(srcPrice.pricelvlsid))
                        continue;

                    var dbPrice = existing.Prices?
                        .FirstOrDefault(p => p.price_lvl_sid == srcPrice.pricelvlsid);

                    // ✅ ONLY update if existing record is found
                    if (dbPrice == null || string.IsNullOrWhiteSpace(dbPrice.sid))
                        continue;

                    updateItem.invnprice.Add(new UpdateInvnPrice
                    {
                        sid = dbPrice.sid, // use existing SID → UPDATE
                        price = srcPrice.price,
                        invnsbsitemsid = itemSid,
                        sbssid = srcPrice.sbssid,
                        pricelvlsid = srcPrice.pricelvlsid,
                        seasonsid = NullIfWhiteSpace(srcPrice.seasonsid) ?? seasonSid
                    });
                }
            }

            //bool hasPriceUpdates = updateItem.invnprice.Count > 0;
            //bool hasValidCost = sourceInv.cost > 0;
            //string? costuploaderReasonSid = null;
            //string? priceuploaderReasonSid = null;
            //string? uploaderReasonSidCached = null;

            //if (hasValidCost)
            //{
            //    uploaderReasonSidCached ??= ReasonRepository.GetUploaderReasonSid();
            //    costuploaderReasonSid = uploaderReasonSidCached;
            //    priceuploaderReasonSid = uploaderReasonSidCached;
            //}
            //else
            //{
            //    uploaderReasonSidCached ??= ReasonRepository.GetUploaderReasonSid();
            //    costuploaderReasonSid = null;
            //    priceuploaderReasonSid = uploaderReasonSidCached;
            //}

            decimal existingCost = existing.cost ?? 0;
            decimal incomingCost = sourceInv.cost;

            // ✅ rule: set reason if ANY cost exists
            bool shouldSetCostReason = existingCost > 0 || incomingCost > 0;

            string? costuploaderReasonSid = null;
            string? priceuploaderReasonSid = null;
            string? uploaderReasonSidCached = null;

            if (shouldSetCostReason)
            {
                uploaderReasonSidCached ??= ReasonRepository.GetUploaderReasonSid();
                costuploaderReasonSid = uploaderReasonSidCached;
            }

            // keep your price logic (or refine separately)
            uploaderReasonSidCached ??= ReasonRepository.GetUploaderReasonSid();
            priceuploaderReasonSid = uploaderReasonSidCached;

            return new UpdateUploadRoot
            {
                data = new List<UpdateUploadData>
                {
                    new UpdateUploadData
                    {
                        OriginApplication = "RProPrismWeb",
                        PrimaryItemDefinition = new UpdatePrimaryItemDefinition
                        {
                            sid = itemSid,
                            dcssid = NullIfWhiteSpace(sourcePrimary.dcssid),
                            vendsid = NullIfWhiteSpace(sourcePrimary.vendsid),
                            description1 = NullIfWhiteSpace(sourcePrimary.description1),
                            description2 = NullIfWhiteSpace(sourcePrimary.description2),
                            attribute = NullIfWhiteSpace(sourcePrimary.attribute),
                            itemsize = NullIfWhiteSpace(sourcePrimary.itemsize)
                        },
                        InventoryItems = new List<UpdateInventoryItem> { updateItem },
                        UpdateStyleDefinition = false,
                        UpdateStyleCost = shouldSetCostReason,
                        UpdateStyleLty = false,
                        UpdateStylePrice = true,
                        DefaultReasonSidForQtyMemo = null,
                        DefaultReasonSidForCostMemo = costuploaderReasonSid, //"774146417000161027",
                        DefaultReasonSidForPriceMemo = priceuploaderReasonSid //"774146417000161027"
                    }
                }
            };
        }

        private string? GetExtendSid(string itemSid)
        {
            if (string.IsNullOrWhiteSpace(itemSid))
                return null;

            const string sql = @"
                SELECT sid
                FROM RPS.invn_sbs_extend
                WHERE invn_sbs_item_sid = :itemSid";

            using var conn = OpenOracleConnection(); // returns OracleConnection
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open(); // only open if not already open

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.BindByName = true;
            cmd.CommandTimeout = 30;

            cmd.Parameters.Add(new OracleParameter("itemSid", OracleDbType.Varchar2)
            {
                Value = itemSid
            });

            var result = cmd.ExecuteScalar();
            return result?.ToString()?.Trim();
        }

        private static string EmptyIfNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private bool HasItemChanges(UploadWorkItem work, ExistingItemInfo existing)
        {
            if (_payloadTable == null)
                return true;

            DataRow? row = _payloadTable.AsEnumerable()
                .FirstOrDefault(r =>
                {
                    string csvUpc = GetDT(_payloadTable, r, "UPC").Trim();
                    string csvAlu = GetDT(_payloadTable, r, "ALU").Trim();

                    return UseUpcMode
                        ? string.Equals(csvUpc, work.Key?.Trim(), StringComparison.OrdinalIgnoreCase)
                        : string.Equals(csvAlu, work.Key?.Trim(), StringComparison.OrdinalIgnoreCase);
                });

            if (row == null)
                return true;

            bool changed = false;

            void Check(string field, bool diff, object? csvValue, object? dbValue)
            {
                if (!diff) return;

                changed = true;
                LogWarn($"CHANGE DETECTED [{work.Key}] Field={field} | CSV='{DisplayCompareValue(csvValue)}' | DB='{DisplayCompareValue(dbValue)}'");
            }

            string csvAlu = GetDT(_payloadTable, row, "ALU");
            string csvUpc = GetDT(_payloadTable, row, "UPC");

            if (UseUpcMode) Check("upc", !AreEqual(csvUpc, existing.upc), csvUpc, existing.upc);
            else Check("alu", !AreEqual(csvAlu, existing.alu), csvAlu, existing.alu);
            Check("vendor_code",!AreEqual(GetDT(_payloadTable, row, "VENDOR_CODE"), existing.vendor_code),GetDT(_payloadTable, row, "VENDOR_CODE"),existing.vendor_code);
            Check("vendor_name",!AreEqual(GetDT(_payloadTable, row, "VENDOR_NAME"), existing.vendor_name),GetDT(_payloadTable, row, "VENDOR_NAME"),existing.vendor_name);

            //var csvDate = Convert.ToDateTime(GetDT(_payloadTable, row, "Date_UDF_1")).Date;
            var dbDate = Convert.ToDateTime(existing.udf1date).Date;

            //AreEqual(Convert.ToDateTime(GetDT(_payloadTable, row, "Date_UDF_1")).ToString("MM/dd/yyyy"),Convert.ToDateTime(existing.udf1date).ToString("MM/dd/yyyy"));


            //Check("udf1date", !AreEqual(GetDT(_payloadTable, row, "Date_UDF_1"), existing.udf1date), GetDT(_payloadTable, row, "Date_UDF_1"), existing.udf1date);
            //Check("udf2date", !AreEqual(GetDT(_payloadTable, row, "Date_UDF_2"), existing.udf2date), GetDT(_payloadTable, row, "Date_UDF_2"), existing.udf2date);
            //Check("udf3date", !AreEqual(GetDT(_payloadTable, row, "Date_UDF_3"), existing.udf3date), GetDT(_payloadTable, row, "Date_UDF_3"), existing.udf3date);

            //Check("lottype",!AreEqual(GetDT(_payloadTable, row, "Lot_Type"),existing.lottype?.ToString()),GetDT(_payloadTable, row, "Lot_Type"),existing.lottype?.ToString());
            //Check("serialtype", !AreEqual(GetDT(_payloadTable, row, "Serial_Type"), existing.serialtype?.ToString()), GetDT(_payloadTable, row, "Serial_Type"), existing.serialtype?.ToString());

            //Check("noninventory", !AreEqual(GetDT(_payloadTable, row, ",NON_INVENTORY"), existing.noninventory?.ToString()), GetDT(_payloadTable, row, ",NON_INVENTORY"), existing.noninventory?.ToString());

            Check("description1", !AreEqual(GetDT(_payloadTable, row, "DESC1"), existing.description1), GetDT(_payloadTable, row, "DESC1"), existing.description1);
            Check("description2", !AreEqual(GetDT(_payloadTable, row, "DESC2"), existing.description2), GetDT(_payloadTable, row, "DESC2"), existing.description2);
            Check("description3", !AreEqual(GetDT(_payloadTable, row, "DESC3"), existing.description3), GetDT(_payloadTable, row, "DESC3"), existing.description3);
            Check("description4", !AreEqual(GetDT(_payloadTable, row, "DESC4"), existing.description4), GetDT(_payloadTable, row, "DESC4"), existing.description4);
            Check("attribute", !AreEqual(GetDT(_payloadTable, row, "ATTR"), existing.attribute), GetDT(_payloadTable, row, "ATTR"), existing.attribute);

            string csvCost = GetDT(_payloadTable, row, "COST");

            bool csvHasValue = decimal.TryParse(csvCost, out decimal costValue);
            decimal? csvCostValue = csvHasValue ? costValue : null;

            Check("cost",!AreEqualDecimal(csvCostValue, existing.cost),csvCost,existing.cost);

            //string csvCost = GetDT(_payloadTable, row, "COST");
            //if (decimal.TryParse(csvCost, out decimal costValue))
            //Check("cost", !AreEqualDecimal(costValue, existing.cost), costValue, existing.cost);
            //else if (!string.IsNullOrWhiteSpace(csvCost))
            //    Check("cost", true, csvCost, existing.cost);

            string csvSpif = GetDT(_payloadTable, row, "SPIF");
            if (decimal.TryParse(csvSpif, out decimal spifValue))
                Check("spif", !AreEqualDecimal(spifValue, existing.spif), spifValue, existing.spif);
            else if (!string.IsNullOrWhiteSpace(csvSpif))
                Check("spif", true, csvSpif, existing.spif);

            foreach (DataColumn c in _payloadTable.Columns)
            {
                if (!c.ColumnName.StartsWith("PRICE_LEVEL", StringComparison.OrdinalIgnoreCase))
                    continue;

                string suffix = c.ColumnName.Substring("PRICE_LEVEL".Length);
                if (!int.TryParse(suffix, out int lvl))
                    continue;

                string csvPriceText = GetDT(_payloadTable, row, c.ColumnName);
                if (string.IsNullOrWhiteSpace(csvPriceText))
                    continue;

                var dbPriceRow = GetExistingPriceByLevel(existing, lvl);
                decimal? dbPrice = dbPriceRow?.price;

                if (decimal.TryParse(csvPriceText, out decimal csvPrice))
                    Check($"price_level_{lvl}", !AreEqualDecimal(csvPrice, dbPrice), csvPrice, dbPrice);
                else
                    Check($"price_level_{lvl}", true, csvPriceText, dbPrice);
            }

            Check("taxcodesid", !AreEqual(GetDT(_payloadTable, row, "TAXCODE_SID"), existing.taxcodesid), GetDT(_payloadTable, row, "TAXCODE_SID"), existing.taxcodesid);
            Check("dcs_code", !AreEqual(GetDT(_payloadTable, row, "DCS_CODE"), existing.dcs_code), GetDT(_payloadTable, row, "DCS_CODE"), existing.dcs_code);

            Check("text1", !AreEqual(GetDT(_payloadTable, row, "TEXT1"), existing.text1), GetDT(_payloadTable, row, "TEXT1"), existing.text1);
            Check("text2", !AreEqual(GetDT(_payloadTable, row, "TEXT2"), existing.text2), GetDT(_payloadTable, row, "TEXT2"), existing.text2);
            Check("text3", !AreEqual(GetDT(_payloadTable, row, "TEXT3"), existing.text3), GetDT(_payloadTable, row, "TEXT3"), existing.text3);
            Check("text4", !AreEqual(GetDT(_payloadTable, row, "TEXT4"), existing.text4), GetDT(_payloadTable, row, "TEXT4"), existing.text4);
            Check("text5", !AreEqual(GetDT(_payloadTable, row, "TEXT5"), existing.text5), GetDT(_payloadTable, row, "TEXT5"), existing.text5);
            Check("text6", !AreEqual(GetDT(_payloadTable, row, "TEXT6"), existing.text6), GetDT(_payloadTable, row, "TEXT6"), existing.text6);
            Check("text7", !AreEqual(GetDT(_payloadTable, row, "TEXT7"), existing.text7), GetDT(_payloadTable, row, "TEXT7"), existing.text7);
            Check("text8", !AreEqual(GetDT(_payloadTable, row, "TEXT8"), existing.text8), GetDT(_payloadTable, row, "TEXT8"), existing.text8);
            Check("text9", !AreEqual(GetDT(_payloadTable, row, "TEXT9"), existing.text9), GetDT(_payloadTable, row, "TEXT9"), existing.text9);
            Check("text10", !AreEqual(GetDT(_payloadTable, row, "TEXT10"), existing.text10), GetDT(_payloadTable, row, "TEXT10"), existing.text10);

            //Check("udf1_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF1"), existing.udf1_string), GetDT(_payloadTable, row, "TEXT_UDF1"), existing.udf1_string);
            //Check("udf2_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF2"), existing.udf2_string), GetDT(_payloadTable, row, "TEXT_UDF2"), existing.udf2_string);
            //Check("udf3_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF3"), existing.udf3_string), GetDT(_payloadTable, row, "TEXT_UDF3"), existing.udf3_string);
            //Check("udf4_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF4"), existing.udf4_string), GetDT(_payloadTable, row, "TEXT_UDF4"), existing.udf4_string);
            //Check("udf5_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF5"), existing.udf5_string), GetDT(_payloadTable, row, "TEXT_UDF5"), existing.udf5_string);


            //Check("udf6_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF6"), existing.udf6_string), GetDT(_payloadTable, row, "TEXT_UDF6"), existing.udf6_string);
            //Check("udf7_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF7"), existing.udf7_string), GetDT(_payloadTable, row, "TEXT_UDF7"), existing.udf7_string);
            //Check("udf8_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF8"), existing.udf8_string), GetDT(_payloadTable, row, "TEXT_UDF8"), existing.udf8_string);
            //Check("udf9_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF9"), existing.udf9_string), GetDT(_payloadTable, row, "TEXT_UDF9"), existing.udf9_string);
            //Check("udf10_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF10"), existing.udf10_string), GetDT(_payloadTable, row, "TEXT_UDF10"), existing.udf10_string);
            //Check("udf11_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF11"), existing.udf11_string), GetDT(_payloadTable, row, "TEXT_UDF11"), existing.udf11_string);
            //Check("udf12_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF12"), existing.udf12_string), GetDT(_payloadTable, row, "TEXT_UDF12"), existing.udf12_string);
            //Check("udf13_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF13"), existing.udf13_string), GetDT(_payloadTable, row, "TEXT_UDF13"), existing.udf13_string);
            //Check("udf14_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF14"), existing.udf14_string), GetDT(_payloadTable, row, "TEXT_UDF14"), existing.udf14_string);
            //Check("udf15_string", !AreEqual(GetDT(_payloadTable, row, "TEXT_UDF15"), existing.udf15_string), GetDT(_payloadTable, row, "TEXT_UDF15"), existing.udf15_string);

            return changed;
        }

        private DateTime? ParseDate(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (DateTimeOffset.TryParse(value.ToString(), out var dto))
                return dto.UtcDateTime;

            if (DateTime.TryParse(value.ToString(), out var dt))
                return dt;

            return null;
        }

        private bool AreEqual(string? a, string? b)
        {
            return string.Equals(NormalizeCompareString(a), NormalizeCompareString(b), StringComparison.OrdinalIgnoreCase);
        }

        private string NormalizeCompareString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string s = value
                .Replace('\u00A0', ' ')
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();

            // ✅ replace comma with space (fix)
            s = s.Replace(",", " ");

            // optional: normalize dots
            s = s.Replace(".", "");

            // normalize multiple spaces → single space
            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            // normalize case
            s = s.ToUpperInvariant();

            return s;
        }

        private bool AreEqualDecimal(decimal? a, decimal? b, int decimals = 4)
        {
            if (!a.HasValue && !b.HasValue) return true;
            if (!a.HasValue || !b.HasValue) return false;

            return Math.Round(a.Value, decimals) == Math.Round(b.Value, decimals);
        }

        private string DisplayCompareValue(object? value)
        {
            if (value == null)
                return "NULL";

            if (value is string s)
                return string.IsNullOrWhiteSpace(s) ? "(empty)" : NormalizeCompareString(s);

            return Convert.ToString(value)?.Trim() ?? "NULL";
        }

        private void EnsureSbssidAndTaxcodesidColumns(DataTable dt)
        {
            if (!dt.Columns.Contains("SBSSID"))
                dt.Columns.Add("SBSSID", typeof(string));

            if (!dt.Columns.Contains("TAXCODE_SID"))
                dt.Columns.Add("TAXCODE_SID", typeof(string));
        }

        private SbsTaxInfo GetSbssidAndTaxcodesidFromDb(string sbsNoText, string taxCode)
        {
            if (string.IsNullOrWhiteSpace(sbsNoText))
                throw new Exception("SBS_No is empty. Unable to resolve SBSSID and TAXCODE_SID.");

            if (string.IsNullOrWhiteSpace(taxCode))
                throw new Exception("Tax code is empty. Unable to resolve TAXCODE_SID.");

            using var conn = OpenOracleConnection();
            using var cmd = conn.CreateCommand();

            cmd.BindByName = true;
            cmd.CommandTimeout = OracleCommandTimeoutSeconds;

            cmd.CommandText = @"
                SELECT
                    s.sid AS sbssid,
                    t.sid AS taxcodesid
                FROM RPS.SUBSIDIARY s
                LEFT JOIN RPS.TAX_CODE t
                    ON t.sbs_sid = s.sid
                   AND t.tax_code = :tax_code
                WHERE s.sbs_no = :sbsNo
                FETCH FIRST 1 ROWS ONLY";

            // ✅ Use correct datatype (string is safer unless you're 100% sure it's numeric)
            cmd.Parameters.Add(":sbsNo", OracleDbType.Varchar2).Value = sbsNoText.Trim();
            cmd.Parameters.Add(":tax_code", OracleDbType.Varchar2).Value = taxCode.Trim();

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                throw new Exception($"No subsidiary/tax code found for SBS_No = {sbsNoText}, TAX_CODE = {taxCode}.");

            return new SbsTaxInfo
            {
                Sbssid = reader["sbssid"]?.ToString()?.Trim() ?? "",
                Taxcodesid = reader["taxcodesid"]?.ToString()?.Trim() ?? ""
            };
        }

        private void UpdateSbssidAndTaxcodesidFromDb(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));

            if (dt.Rows.Count == 0)
            {
                LogWarn("[SBSSID/TAXCODE] DataTable is empty.");
                return;
            }

            string sbsCol =
                dt.Columns.Contains("SBS_No") ? "SBS_No" :
                dt.Columns.Contains("SBS_No.") ? "SBS_No." :
                "";

            if (string.IsNullOrWhiteSpace(sbsCol))
                throw new Exception("CSV column 'SBS_No' / 'SBS_No.' not found.");

            // Optional: ensure tax_code column exists in CSV
            if (!dt.Columns.Contains("TAX_CODE"))
                throw new Exception("CSV column 'TAX_CODE' not found.");

            EnsureSbssidAndTaxcodesidColumns(dt);

            var cache = new Dictionary<string, SbsTaxInfo>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                try
                {
                    string sbsNoText = dt.Rows[i][sbsCol]?.ToString()?.Trim() ?? "";
                    string taxCode = dt.Rows[i]["TAX_CODE"]?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(sbsNoText))
                    {
                        //LogWarn($"[Row {i + 1}] [SBSSID/TAXCODE] Empty SBS_No. Skipped.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(taxCode))
                    {
                        //LogWarn($"[Row {i + 1}] [SBSSID/TAXCODE] Empty TAX_CODE. Skipped.");
                        continue;
                    }

                    string cacheKey = $"{sbsNoText}|{taxCode}";

                    if (!cache.TryGetValue(cacheKey, out var info))
                    {
                        info = GetSbssidAndTaxcodesidFromDb(sbsNoText, taxCode);
                        cache[cacheKey] = info;
                    }

                    dt.Rows[i]["SBSSID"] = info.Sbssid;
                    dt.Rows[i]["TAXCODE_SID"] = info.Taxcodesid;

                    //LogInfo($"[Row {i + 1}] [SBSSID/TAXCODE] SBS_No={sbsNoText}, TAX_CODE={taxCode} -> SBSSID={info.Sbssid}, TAXCODE_SID={info.Taxcodesid}");
                }
                catch (Exception ex)
                {
                    LogError($"[Row {i + 1}] [SBSSID/TAXCODE] Failed: {ex.Message}");
                }
            }

            LogInfo("[SBSSID/TAXCODE] Lookup completed.");
        }
    }
}