using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;

namespace SmallBusinessLendingDesktop
{
    public partial class Form1: Form
    {
        
             private List<string> requiredHeaders = new List<string>
        {
            "uid","app_date","app_method","app_recipient","ct_credit_product",
            "ct_credit_product_ff","ct_guarantee","ct_guarantee_ff","ct_loan_term_flag",
            "ct_loan_term","credit_purpose","credit_purpose_ff","amount_applied_for_flag",
            "amount_applied_for","amount_approved","action_taken","action_taken_date","denial_reasons",
            "denial_reasons_ff","pricing_interest_rate_type","pricing_init_rate_period","pricing_fixed_rate",
            "pricing_adj_margin","pricing_adj_index_name","pricing_adj_index_name_ff","pricing_adj_index_value",
            "pricing_origination_charges","pricing_broker_fees","pricing_initial_charges","pricing_mca_addcost_flag",
            "pricing_mca_addcost","pricing_prepenalty_allowed","pricing_prepenalty_exists","census_tract_adr_type",
            "census_tract_number","gross_annual_revenue_flag","gross_annual_revenue","naics_code_flag","naics_code",
            "number_of_workers","time_in_business_type","time_in_business","business_ownership_status",
            "num_principal_owners_flag","num_principal_owners","po_1_ethnicity","po_1_ethnicity_ff",
            "po_1_race","po_1_race_anai_ff","po_1_race_asian_ff","po_1_race_baa_ff","po_1_race_pi_ff",
            "po_1_gender_flag","po_1_gender_ff","po_2_ethnicity","po_2_ethnicity_ff","po_2_race",
            "po_2_race_anai_ff","po_2_race_asian_ff","po_2_race_baa_ff","po_2_race_pi_ff",
            "po_2_gender_flag","po_2_gender_ff","po_3_ethnicity","po_3_ethnicity_ff",
            "po_3_race","po_3_race_anai_ff","po_3_race_asian_ff","po_3_race_baa_ff",
            "po_3_race_pi_ff","po_3_gender_flag","po_3_gender_ff","po_4_ethnicity",
            "po_4_ethnicity_ff","po_4_race","po_4_race_anai_ff","po_4_race_asian_ff",
            "po_4_race_baa_ff","po_4_race_pi_ff","po_4_gender_flag","po_4_gender_ff"
        };

        private List<ValidationError> validationErrors = new List<ValidationError>();

        public Form1()
        {
            InitializeComponent();
            txtLEI.TextChanged += new EventHandler(txtLEI_TextChanged); // Attach event handler
            InitializeDataGridView();

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            txtLEI.Text = Properties.Settings.Default.SavedLEI; // Load saved LEI
                                                                // For StartDate:
            DateTime savedStartDate = Properties.Settings.Default.StartDate;
            if (savedStartDate < StartDate.MinDate)
                StartDate.Value = StartDate.MinDate;
            else
                StartDate.Value = savedStartDate;

            // For Enddate:
            DateTime savedEndDate = Properties.Settings.Default.Enddate;
            if (savedEndDate < Enddate.MinDate)
                Enddate.Value = Enddate.MinDate;
            else
                Enddate.Value = savedEndDate;
            //dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void txtLEI_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SavedLEI = txtLEI.Text.Trim();
            Properties.Settings.Default.Save(); // Save the value persistently
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.StartDate = StartDate.Value;
            Properties.Settings.Default.Save(); // Save the value persistently
           // Properties.Settings.Default.Enddate = Enddate.DateTime.Trim();
            //Properties.Settings.Default.Save(); // Save the value persistently

        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Enddate = Enddate.Value;
            Properties.Settings.Default.Save(); // Save the value persistently
                                                // Properties.Settings.Default.Enddate = Enddate.DateTime.Trim();
                                                //Properties.Settings.Default.Save(); // Save the value persistently

        }

        private void InitializeDataGridView()
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.Columns.Clear();

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Row", DataPropertyName = "RowNumber" });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Column", DataPropertyName = "ColumnName" });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Validation ID", DataPropertyName = "ValidationID" });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Error Message", DataPropertyName = "ErrorMessage" });

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    ImportCsvFile(filePath);
                }
            }
        }

        private void ImportCsvFile(string filePath)
        {

            string userEnteredLEI = txtLEI?.Text.Trim();

            if (string.IsNullOrWhiteSpace(userEnteredLEI) || userEnteredLEI.Length != 20)
            {
                MessageBox.Show("Please enter a valid 20-character Legal Entity Identifier (LEI) before importing the file.",
                                "LEI Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // Stop the import process
            }

            validationErrors.Clear();

            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("File not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

                if (lines.Length < 2)
                {
                    MessageBox.Show("File must contain at least a header and one data row.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string[] headers = lines[0].Split(',');

                if (!ValidateHeader(headers, out string headerError))
                {
                    MessageBox.Show(headerError, "Header Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    string[] fields = ParseCsvLine(lines[i]);

                    if (fields.Length != headers.Length)
                    {
                        validationErrors.Add(new ValidationError(i + 1, "General", "", "Column count mismatch."));
                        continue;
                    }

                    ValidateRow(i + 1, fields);
                }
                validationErrors.Sort();
                dataGridView1.DataSource = null;
                dataGridView1.DataSource = validationErrors;

                if (!validationErrors.Any())
                {
                    MessageBox.Show("File imported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateHeader(string[] headers, out string errorMessage)
        {
            if (!headers.SequenceEqual(requiredHeaders))
            {
                errorMessage = "Header row does not match the required format.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private string[] ParseCsvLine(string line)
        {
            Regex csvRegex = new Regex(@"(?:^|,)(?:""(?<Value>(?:[^""]|"""")*)""|(?<Value>[^,""]*))", RegexOptions.Compiled);
            return csvRegex.Matches(line).Cast<Match>().Select(m => m.Groups["Value"].Value.Replace("\"\"", "\"").Trim()).ToArray();
        }

        // Store seen UIDs for uniqueness validation
        private HashSet<string> seenUids = new HashSet<string>(); // Track unique UIDs

        // Define Legal Entity Identifier (LEI) for validation

        private void ValidateRow(int rowNumber, string[] fields)
        {
            // Example validation for 'uid' field (Column A, Index 0)

            {
                if (fields.Length < 1)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "uid", "", "Missing Unique Identifier - Column A must be present."));

                }

                string uid = fields[0].Trim(); // Column A (uid)
                string userEnteredLEI = txtLEI?.Text.Trim() ?? ""; // Get LEI from TextBox safely

                if (string.IsNullOrWhiteSpace(userEnteredLEI) || userEnteredLEI.Length != 20)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "uid", "", "Missing or Incorrect LEI - Please enter a valid 20-character LEI in the form."));

                }

                if (uid.Length < 21 || uid.Length > 45)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "uid", "E0001", "'uid' must be between 21 and 45 characters."));
                }

                if (!Regex.IsMatch(uid, "^[A-Z0-9]+$"))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "uid", "E0002", "'uid' may only contain uppercase letters (A-Z) and numbers (0-9), with no special characters."));
                }

                if (!uid.StartsWith(userEnteredLEI))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "uid", "W0003", $"Invalid LEI - 'uid' must start with: {userEnteredLEI}."));
                }

                if (seenUids.Contains(uid))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "uid", "E3000", "Duplicate UID - Each Unique Identifier must be used only once in the dataset."));
                }
                else
                {
                    seenUids.Add(uid);
                }

                ////////////////////////////////////////////////////////////////////////////////////////////
                //Applicaiton Date

                if (fields.Length < 2) // Assuming 'app_date' is Column B (Index 1)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "app_date", "", "Missing Application Date - Column B must be present."));

                }

                string app_date = fields[1].Trim(); // Column B (app_date)

                // ✅ Validation: Required field (must not be empty)
                if (string.IsNullOrWhiteSpace(app_date))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "app_date", "", "Application Date is required and cannot be empty."));

                }

                // ✅ Validation: Check YYYYMMDD format (must be exactly 8 digits)
                if (!Regex.IsMatch(app_date, @"^\d{8}$"))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "app_date", "", "Invalid Format - Application Date must be in YYYYMMDD format (e.g., 20251001)."));

                }

                // ✅ Validation: Ensure it is a real calendar date
                if (!IsValidDateYYYYMMDD(app_date))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "app_date", "E0020", "Application Date must be a real calendar date (e.g., 20251001)."));

                }
                ///////////////////////////////////////////////////////////////////////////////////////////
                //application method 

                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 3) // Assuming 'app_method' is Column C (Index 2)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "app_method", "", "Missing Application Method - Column C must be present."));

                }

                string app_method = fields[2]?.Trim() ?? ""; // Handle NaN values

                // ✅ Check if empty or NaN (string.IsNullOrWhiteSpace also handles NaN)
                if (string.IsNullOrWhiteSpace(app_method))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "app_method", "", "Application Method is required and cannot be empty."));

                }

                // ✅ Allowed values
                HashSet<string> validAppMethods = new HashSet<string> { "1", "2", "3", "4" };

                // ✅ Ensure the value is one of the allowed codes
                if (!validAppMethods.Contains(app_method))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "app_method", "E0040", "Application Method - Must be 1, 2, 3, or 4."));
                }

                //////////////////////////////////////////////////////////////////////////////////////////////

                if (fields.Length < 4) // Assuming 'app_recipient' is Column D (Index 3)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "app_recipient", "", "Missing Application Recipient - Column D must be present."));

                }

                string app_recipient = fields[3]?.Trim() ?? ""; // Handle null values safely

                // ✅ Required field: Ensure it is not empty (handles hidden spaces too)
                if (string.IsNullOrWhiteSpace(app_recipient))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "app_recipient", "", "Application Recipient is required and cannot be empty."));

                }

                // ✅ Allowed values
                HashSet<string> validAppRecipients = new HashSet<string> { "1", "2" };

                // ✅ Ensure the value is one of the allowed codes
                if (!validAppRecipients.Contains(app_recipient))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "app_recipient", "E0060", "Application Recipient - Must be 1 or 2."));
                }

                ////////////////////////////////////////////////////////////////////////////////////////

                if (fields.Length < 5) // Assuming 'ct_credit_product' is Column E (Index 4)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_credit_product", "", "Missing Credit Product - Column E must be present."));

                }

                string ct_credit_product = fields[4]?.Trim() ?? ""; // Handle null values safely

                // ✅ Required field: Ensure it is not empty (handles hidden spaces too)
                if (string.IsNullOrWhiteSpace(ct_credit_product))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_credit_product", "", "Credit Product is required and cannot be empty."));

                }

                // ✅ Allowed values
                HashSet<string> validCreditProducts = new HashSet<string> { "1", "2", "3", "4", "5", "6", "7", "8", "977", "988" };

                // ✅ Ensure the value is one of the allowed codes
                if (!validCreditProducts.Contains(ct_credit_product))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_credit_product", "E0080", "Credit Product - Must be 1, 2, 3, 4, 5, 6, 7, 8, 977, or 988."));
                }
                //////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 6) // Assuming 'ct_credit_product_ff' is Column F (Index 5)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_credit_product_ff", "", "Missing field - Column F must be present."));

                }

                string ctCreditProduct = fields[4]?.Trim() ?? ""; // Column E (ct_credit_product)
                string ctCreditProductFF = fields[5]?.Trim() ?? ""; // Column F (ct_credit_product_ff)

                // ✅ Validation: Check if the text exceeds 300 characters
                if (ctCreditProductFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_credit_product_ff", "E0100", "Free-form text field for other credit products- Must not exceed 300 characters."));
                }

                // ✅ Validation: If ct_credit_product is 977, ct_credit_product_ff is required
                if (ctCreditProduct == "977" && string.IsNullOrWhiteSpace(ctCreditProductFF))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_credit_product_ff", "E2000", "Required field - Must specify other credit product if credit product is 977."));
                }

                // ✅ Validation: If ct_credit_product is NOT 977, ct_credit_product_ff must be blank
                if (ctCreditProduct != "977" && !string.IsNullOrWhiteSpace(ctCreditProductFF))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_credit_product_ff", "E2000", "Invalid input - Must be left blank unless credit product is 977."));
                }
                /////////////////////////////////////////////////////////////////////////////////

                if (fields.Length < 7) // Assuming 'ct_guarantee' is Column G (Index 6)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_guarantee", "", "Missing Guarantee Type - Column G must be present."));

                }

                string ct_Guarantee = fields[6]?.Trim() ?? ""; // Column G (ct_guarantee)

                // ✅ Required field: Ensure it is not empty
                if (string.IsNullOrWhiteSpace(ct_Guarantee))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_guarantee", "", "Guarantee Type is required and cannot be empty."));

                }

                // ✅ Split values by semicolon (;), trim spaces
                string[] guaranteeValues = ct_Guarantee.Split(';').Select(v => v.Trim()).ToArray();

                // ✅ Ensure there are at least 1 and at most 5 values
                if (guaranteeValues.Length < 1 || guaranteeValues.Length > 5)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_guarantee", "E0121", "Guarantee Type must contain between 1 and 5 values, separated by semicolons."));
                }

                // ✅ Allowed guarantee codes
                HashSet<string> validGuaranteeCodes = new HashSet<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "977", "999" };

                // ✅ Ensure all values are valid
                foreach (string value in guaranteeValues)
                {
                    if (!validGuaranteeCodes.Contains(value))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "ct_guarantee", "E0120", $"Invalid Guarantee Code '{value}' - Must be 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 977, or 999."));
                    }
                }

                // ✅ Ensure values are unique (no duplicates)
                if (guaranteeValues.Distinct().Count() != guaranteeValues.Length)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_guarantee", "W0123", "Duplicate values found - Guarantee Type should not contain repeated codes."));
                }

                // ✅ Special rule: If `999` is present, it must be the only value
                if (guaranteeValues.Contains("999") && guaranteeValues.Length > 1)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_guarantee", "W0122", "Invalid Combination - If code 999 is reported, no other values should be present."));
                }
                ////////////////////////////////////////////////////////////////////
                if (fields.Length < 8) // Assuming 'ct_guarantee_ff' is Column H (Index 7)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_guarantee_ff", "", "Missing field - Column H must be present."));

                }

                string ctGuarantee = fields[6]?.Trim() ?? ""; // Column G (ct_guarantee)
                string ctGuaranteeFF = fields[7]?.Trim() ?? ""; // Column H (ct_guarantee_ff)

                // ✅ Validation: Ensure text does not exceed 300 characters
                if (ctGuaranteeFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_guarantee_ff", "E0140", "Free-form text field for other guarantee' - Must not exceed 300 characters."));
                }

                // ✅ Check if `ct_guarantee` contains `977`
                bool contains977 = ctGuarantee.Split(';').Select(v => v.Trim()).Contains("977");

                // ✅ Validation: If `ct_guarantee` contains `977`, `ct_guarantee_ff` is required
                if (contains977 && string.IsNullOrWhiteSpace(ctGuaranteeFF))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_guarantee_ff", "E2001", "Required field - Must specify other guarantee if type of guarantee contains 977."));
                }

                // ✅ Validation: If `ct_guarantee` does not contain `977`, `ct_guarantee_ff` must be blank
                if (!contains977 && !string.IsNullOrWhiteSpace(ctGuaranteeFF))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_guarantee_ff", "E2001", "Invalid input - Must be left blank unless type of guarantee contains 977."));
                }
                ///////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 9) // Assuming 'ct_loan_term_flag' is Column I (Index 8)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term_flag", "", "Missing Loan Term Flag - Column I must be present."));

                }

                string ctLoanTermFlag = fields[8]?.Trim() ?? ""; // Column I (ct_loan_term_flag)

                // ✅ Required field: Ensure it is not empty (handles hidden spaces too)
                if (string.IsNullOrWhiteSpace(ctLoanTermFlag))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term_flag", "", "Loan Term Flag is required and cannot be empty."));

                }

                // ✅ Allowed values
                HashSet<string> validLoanTermFlags = new HashSet<string> { "900", "988", "999" };

                // ✅ Ensure the value is one of the allowed codes
                if (!validLoanTermFlags.Contains(ctLoanTermFlag))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term_flag", "E0160", "Loan Term Flag - Must be 900, 988, or 999."));
                }

                ///////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 10) // Assuming 'ct_loan_term' is Column J (Index 9)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term", "", "Missing Loan Term - Column J must be present."));

                }

                string ct_Loan_Term_Flag = fields[8]?.Trim() ?? ""; // Column I (ct_loan_term_flag)
                string ctLoanTerm = fields[9]?.Trim() ?? ""; // Column J (ct_loan_term)

                // ✅ Check if ct_loan_term_flag is '900' (conditionally required)
                bool isLoanTermRequired = ct_Loan_Term_Flag == "900";

                // ✅ If loan term is required but missing, throw an error
                if (isLoanTermRequired && string.IsNullOrWhiteSpace(ctLoanTerm))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term", "E2004", "Loan Term is required when 'Loan Term Flag' is 900."));

                }

                // ✅ If loan term is not required but present, it must be blank
                if (!isLoanTermRequired && !string.IsNullOrWhiteSpace(ctLoanTerm))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term", "E2004", "Loan Term must be left blank unless 'Loan Term Flag' is 900."));

                }

                // ✅ If loan term is present, ensure it is a whole number
                if (!string.IsNullOrWhiteSpace(ctLoanTerm))
                {
                    if (!int.TryParse(ctLoanTerm, out int loanTerm))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term", "E0180", "Loan Term - Must be a whole number."));

                    }

                    // ✅ Ensure loan term is within valid range (1 to 1199)
                    if (loanTerm < 1)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term", "E0181", "Loan Term - Must be greater than or equal to 1."));
                    }
                    else if (loanTerm >= 1200)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term", "W0182", "Invalid Loan Term - Must be less than 1200 months (100 years)."));
                    }
                }

                // Retrieve the values from your data source (e.g., CSV fields)
                string ctCredit_Product = fields[4]?.Trim() ?? "";
                string ctLoanTerm_Flag = fields[8]?.Trim() ?? "";

                // Validation logic based on the provided conditions:
                // If ct_credit_product is equal to "1" or "2", then ct_loan_term_flag must NOT equal "999".
                if (ctCredit_Product == "1" || ctCredit_Product == "2")
                {
                    if (ctLoanTerm_Flag == "999")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term_flag", "E2003", "Invalid combination: When ct_credit_product is 1 or 2, ct_loan_term_flag cannot be 999."));

                    }
                }
                // Else, if ct_credit_product is equal to "988", then ct_loan_term_flag must equal "999".
                else if (ctCredit_Product == "988")
                {
                    if (ctLoanTerm_Flag != "999")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "ct_loan_term_flag", "E2003", "Invalid combination: When ct_credit_product is 988, ct_loan_term_flag must be 999."));
                    }
                }



                ////////////////////////////////////////////////////////////
                if (fields.Length < 11) // Assuming 'credit_purpose' is Column K (Index 10)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "credit_purpose", "", "Missing Credit Purpose - Column K must be present."));

                }

                string creditPurpose = fields[10]?.Trim() ?? ""; // Column K (credit_purpose)

                // ✅ Required field: Ensure it is not empty
                if (string.IsNullOrWhiteSpace(creditPurpose))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "credit_purpose", "", "Credit Purpose is required and cannot be empty."));

                }

                // ✅ Split values by semicolon (;), trim spaces
                string[] purposeValues = creditPurpose.Split(';').Select(v => v.Trim()).ToArray();

                // ✅ Ensure there are at least 1 and at most 3 values
                if (purposeValues.Length < 1 || purposeValues.Length > 3)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "credit_purpose", "E0201", "Credit Purpose must contain between 1 and 3 values, separated by semicolons."));
                }

                // ✅ Allowed credit purpose codes
                HashSet<string> validCreditPurposeCodes = new HashSet<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "977", "988", "999" };

                // ✅ Ensure all values are valid
                foreach (string value in purposeValues)
                {
                    if (!validCreditPurposeCodes.Contains(value))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "credit_purpose", "E0200", "Credit Purpose Code '{value}' - Must be 1-11, 977, 988, or 999."));
                    }
                }

                // ✅ Ensure values are unique (no duplicates)
                if (purposeValues.Distinct().Count() != purposeValues.Length)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "credit_purpose", "W0203", "Duplicate values found - Credit Purpose should not contain repeated codes."));
                }

                // ✅ Special rule: If `988` or `999` is present, it must be the only value
                if ((purposeValues.Contains("988") || purposeValues.Contains("999")) && purposeValues.Length > 1)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "credit_purpose", "W0202", "Invalid Combination - If code 988 or 999 is reported, no other values should be present."));
                }
                ////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 12) // Assuming 'credit_purpose_ff' is Column L (Index 11)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "credit_purpose_ff", "", "Missing field - Column L must be present."));

                }

                string credit_Purpose = fields[10]?.Trim() ?? ""; // Column K (credit_purpose)
                string creditPurposeFF = fields[11]?.Trim() ?? ""; // Column L (credit_purpose_ff)

                // ✅ Ensure text does not exceed 300 characters
                if (creditPurposeFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "credit_purpose_ff", "E0220", "'Free-form text field for other credit purpose - Must not exceed 300 characters."));
                }

                // ✅ Check if `credit_purpose` contains `977`
                bool contains_977 = !string.IsNullOrWhiteSpace(credit_Purpose) &&
                                   credit_Purpose.Split(';')
                                                .Select(v => v.Trim())
                                                .Contains("977");

                // ✅ If `credit_purpose` contains `977`, `credit_purpose_ff` is required
                if (contains_977 && string.IsNullOrWhiteSpace(creditPurposeFF))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "credit_purpose_ff", "E2005", "Required field - Must specify other credit purpose if 'credit purpose' contains 977."));
                }

                // ✅ If `credit_purpose` does not contain `977`, `credit_purpose_ff` must be blank
                if (!contains_977 && !string.IsNullOrWhiteSpace(creditPurposeFF))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "credit_purpose_ff", "E2005", "Invalid input - Must be left blank unless 'credit purpose' contains 977."));
                }
                //////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 13) // Assuming 'amount_applied_for_flag' is Column M (Index 12)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "amount_applied_for_flag", "", "Missing Amount Applied For Flag - Column M must be present."));

                }

                string amountAppliedForFlag = fields[12]?.Trim() ?? ""; // Column M (amount_applied_for_flag)

                // ✅ Required field: Ensure it is not empty (handles hidden spaces too)
                if (string.IsNullOrWhiteSpace(amountAppliedForFlag))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "amount_applied_for_flag", "", "Amount Applied For Flag is required and cannot be empty."));

                }

                // ✅ Allowed values
                HashSet<string> validAmountFlags = new HashSet<string> { "900", "988", "999" };

                // ✅ Ensure the value is one of the allowed codes
                if (!validAmountFlags.Contains(amountAppliedForFlag))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "amount_applied_for_flag", "E0240", "Amount Applied For Flag - Must be 900, 988, or 999."));
                }
                ///////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 14) // Assuming 'amount_applied_for' is Column N (Index 13)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "amount_applied_for", "", "Missing Amount Applied For - Column N must be present."));

                }

                string amount_Applied_For_Flag = fields[12]?.Trim() ?? ""; // Column M (amount_applied_for_flag)
                string amountAppliedFor = fields[13]?.Trim() ?? ""; // Column N (amount_applied_for)

                // ✅ Check if `amount_applied_for_flag` is '900' (conditionally required)
                bool isAmountRequired = amount_Applied_For_Flag == "900";

                // ✅ If required but missing, throw an error
                if (isAmountRequired && string.IsNullOrWhiteSpace(amountAppliedFor))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "amount_applied_for", "E2007", "Amount Applied For is required when 'Amount Applied For Flag' is 900."));

                }

                // ✅ If not required but present, it must be blank
                if (!isAmountRequired && !string.IsNullOrWhiteSpace(amountAppliedFor))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "amount_applied_for", "E2007", "Amount Applied For must be left blank unless 'Amount Applied For Flag' is 900."));

                }

                // ✅ If present, ensure it is a valid numeric value
                if (!string.IsNullOrWhiteSpace(amountAppliedFor))
                {
                    if (!decimal.TryParse(amountAppliedFor, out decimal amount))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "amount_applied_for", "E0260", "Amount Applied For - Must be a numeric value."));

                    }

                    // ✅ Ensure amount is greater than 0
                    if (amount <= 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "amount_applied_for", "E0261", "Amount Applied For - Must be greater than 0."));
                    }
                }
                ////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 15) // Assuming 'amount_approved' is Column P (Index 14)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "amount_approved", "", "Missing Amount Approved - Column O must be present."));

                }

                string amountApproved = fields[14]?.Trim() ?? ""; // Column O (amount_approved)
                string actionTaken = fields[15]?.Trim() ?? ""; // Column P (action_taken)


                // ✅ Check if `action_taken` is '1' or '2' (conditionally required)
                bool amount_Approved = actionTaken == "1" || actionTaken == "2";

                // ✅ If required but missing, throw an error
                if (amount_Approved && string.IsNullOrWhiteSpace(amountApproved))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "amount_approved", "E2008", "Amount Approved is required when 'Action Taken' is 1 or 2."));

                }

                // ✅ If not required but present, it must be blank
                if (!amount_Approved && !string.IsNullOrWhiteSpace(amountApproved))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "amount_approved", "E2008", "Amount Approved must be left blank unless 'Action Taken' is 1 or 2."));

                }

                // ✅ If present, ensure it is a valid numeric value
                if (!string.IsNullOrWhiteSpace(amountApproved))
                {
                    if (!decimal.TryParse(amountApproved, out decimal approvedamount))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "amount_approved", "E0280", "Amount Approved - Must be a numeric value."));

                    }

                    // ✅ Ensure amount is greater than 0
                    if (approvedamount <= 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "amount_approved", "E0281", "Amount Approved - Must be greater than 0."));
                    }
                }
                /////////////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 16) // Assuming 'action_taken' is Column O (Index 14)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "action_taken", "", "Missing Action Taken - Column O must be present."));

                }

                string action_Taken = fields[15]?.Trim() ?? ""; // Column O (action_taken)

                // ✅ Required field: Ensure it is not empty (handles hidden spaces too)
                if (string.IsNullOrWhiteSpace(action_Taken))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "action_taken", "", "Action Taken is required and cannot be empty."));

                }

                // ✅ Allowed values
                HashSet<string> validActionTakenCodes = new HashSet<string> { "1", "2", "3", "4", "5" };

                // ✅ Ensure the value is one of the allowed codes
                if (!validActionTakenCodes.Contains(action_Taken))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "action_taken", "E0300", "Action Taken - Must be 1, 2, 3, 4, or 5."));
                }

                //////////////////////////////////////////////////////////////////////////////////////


                if (fields.Length < 17) // Assuming 'action_taken_date' is Column P (Index 15)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "action_taken_date", "", "Missing Action Taken Date - Column P must be present."));

                }

                string actionTakenDate = fields[16]?.Trim() ?? ""; // Column Q (action_taken_date)

                // ✅ Required field: Ensure it is not empty
                if (string.IsNullOrWhiteSpace(actionTakenDate))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "action_taken_date", "", "Action Taken Date is required and cannot be empty."));

                }

                // ✅ Check YYYYMMDD format (must be exactly 8 digits)
                if (!Regex.IsMatch(actionTakenDate, @"^\d{8}$"))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "action_taken_date", "", "Invalid Format - Action Taken Date must be in YYYYMMDD format (e.g., 20251001)."));

                }

                // ✅ Convert to DateTime and check validity
                if (!DateTime.TryParseExact(actionTakenDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "action_taken_date", "E0320", "Action Taken Date must be a real calendar date (e.g., 20251001)."));

                }

               
                // Assume dtpStartDate and dtpEndDate hold the user-entered reporting period.
                DateTime reportingStartDate = StartDate.Value;
                DateTime reportingEndDate = Enddate.Value;

                // Validate that parsedDate falls within the user-defined reporting period.
                if (parsedDate < reportingStartDate || parsedDate > reportingEndDate)
                {
                    validationErrors.Add(new ValidationError(
                        rowNumber,
                        "action_taken_date",
                        "E0321",
                        $"Action Taken Date must be between {reportingStartDate:MMMM d, yyyy} and {reportingEndDate:MMMM d, yyyy}."
                    ));
                }


                // Retrieve the date strings from the appropriate fields (update the indexes as needed)
                string actionTakenDateStr = fields[16]?.Trim() ?? "";
                string appDateStr = fields[1]?.Trim() ?? "";

                // Attempt to parse the dates
                if (DateTime.TryParse(actionTakenDateStr, out DateTime actionTaken_Date) &&
                    DateTime.TryParse(appDateStr, out DateTime appDate))
                {
                    // If action_taken_date is earlier than app_date, report an error.
                    if (actionTaken_Date < appDate)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "action_taken_date", "E2009", "Action Taken Date cannot be earlier than Application Date."));

                    }
                }


                ///////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 18) // Assuming 'denial_reasons' is Column Q (Index 16)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "denial_reasons", "", "Missing Denial Reasons - Column Q must be present."));

                }

                string denialReasons = fields[17]?.Trim() ?? ""; // Column R (denial_reasons)

                // ✅ Required field: Ensure it is not empty
                if (string.IsNullOrWhiteSpace(denialReasons))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "denial_reasons", "", "Denial Reasons is required and cannot be empty."));

                }

                // ✅ Split values by semicolon (;), trim spaces
                string[] reasonValues = denialReasons.Split(';').Select(v => v.Trim()).ToArray();

                // ✅ Ensure there are at least 1 and at most 4 values
                if (reasonValues.Length < 1 || reasonValues.Length > 4)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "denial_reasons", "E0341", "Denial Reasons must contain between 1 and 4 values, separated by semicolons."));
                }

                // ✅ Allowed denial reason codes
                HashSet<string> validDenialReasonCodes = new HashSet<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "977", "999" };

                // ✅ Ensure all values are valid
                foreach (string value in reasonValues)
                {
                    if (!validDenialReasonCodes.Contains(value))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "denial_reasons", "E0340", "Denial Reason Code '{value}' - Must be 1-9, 977, or 999."));
                    }
                }

                // ✅ Ensure values are unique (no duplicates)
                if (reasonValues.Distinct().Count() != reasonValues.Length)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "denial_reasons", "W0341", "Duplicate values found - Denial Reasons should not contain repeated codes."));
                }

                // ✅ Special rule: If `999` is present, it must be the only value
                if (reasonValues.Contains("999") && reasonValues.Length > 1)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "denial_reasons", "W0340", "Invalid Combination - If code 999 is reported, no other values should be present."));
                }

                // Retrieve the vaues (adjust the indexes as needed)
                string actionTaken4 = fields[15]?.Trim() ?? "";
                string denialReasons1 = fields[17]?.Trim() ?? "";

                // Validation Logic:
                // If action_taken equals "3", then denial_reasons must not contain code "999".
                // Otherwise (action_taken is not "3"), denial_reasons must equal "999".

                if (actionTaken4 == "3")
                {
                    // Split denialReasons into individual codes (in case multiple codes are provided)
                    var denialCodes = denialReasons1
                                      .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(code => code.Trim())
                                      .ToList();

                    // If any code equals "999", report an error.
                    if (denialCodes.Contains("999"))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "denial_reasons", "E2011", "Error: When action_taken is 3, denial_reasons must not contain code 999."));

                    }
                }
                else // actionTaken is not "3"
                {
                    // In this case, denial_reasons must equal "999" exactly.
                    if (denialReasons1 != "999")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "denial_reasons", "E2011", "Error: When action_taken is not 3, denial_reasons must equal 999."));

                    }
                }

                /////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 19) // Assuming 'denial_reasons_ff' is Column R (Index 17)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "denial_reasons_ff", "", "Missing field - Column R must be present."));

                }

                string denial_Reasons = fields[17]?.Trim() ?? ""; // Column Q (denial_reasons)
                string denialReasonsFF = fields[18]?.Trim() ?? ""; // Column R (denial_reasons_ff)

                // ✅ Ensure text does not exceed 300 characters
                if (denialReasonsFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "denial_reasons_ff", "E0360", "Free-form text field for other denial reason - Must not exceed 300 characters."));
                }

                // ✅ Check if `denial_reasons` contains `977`
                bool contains_9777 = !string.IsNullOrWhiteSpace(denial_Reasons) &&
                                      denial_Reasons.Split(';')
                                                   .Select(v => v.Trim())
                                                   .Contains("977");

                // ✅ If `denial_reasons` contains `977`, `denial_reasons_ff` is required
                if (contains_9777 && string.IsNullOrWhiteSpace(denialReasonsFF))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "denial_reasons_ff", "E2012", "Required field - Must specify other denial reason if 'denial reasons' contains 977."));
                }

                // ✅ If `denial_reasons` does not contain `977`, `denial_reasons_ff` must be blank
                if (!contains_9777 && !string.IsNullOrWhiteSpace(denialReasonsFF))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "denial_reasons_ff", "E2012", "Invalid input - Must be left blank unless 'denial reasons' contains 977."));
                }
                ////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 20) // Assuming 'pricing_interest_rate_type' is Column S (Index 18)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_interest_rate_type", "", "Missing Pricing Interest Rate Type - Column S must be present."));

                }

                string pricingInterestRateType = fields[19]?.Trim() ?? ""; // Column S (pricing_interest_rate_type)

                // ✅ Required field: Ensure it is not empty (handles hidden spaces too)
                if (string.IsNullOrWhiteSpace(pricingInterestRateType))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_interest_rate_type", "", "Pricing Interest Rate Type is required and cannot be empty."));

                }

                // ✅ Allowed values
                HashSet<string> validInterestRateTypes = new HashSet<string> { "1", "2", "3", "4", "5", "6", "999" };

                // ✅ Ensure the value is one of the allowed codes
                if (!validInterestRateTypes.Contains(pricingInterestRateType))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_interest_rate_type", "E0380", "Pricing Interest Rate Type - Must be 1, 2, 3, 4, 5, 6, or 999."));
                }
                ///////////////////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 21) // Assuming 'pricing_init_rate_period' is Column T (Index 19)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_init_rate_period", "", "Missing Initial Rate Period - Column T must be present."));

                }

                string pricing_InterestRate_Type = fields[19]?.Trim() ?? ""; // Column S (pricing_interest_rate_type)
                string pricingInitRatePeriod = fields[20]?.Trim() ?? ""; // Column T (pricing_init_rate_period)

                // ✅ Check if `pricing_interest_rate_type` is '3, 4, 5, or 6' (conditionally required)
                bool isRatePeriodRequired = new HashSet<string> { "3", "4", "5", "6" }.Contains(pricing_InterestRate_Type);

                // ✅ If required but missing, throw an error
                if (isRatePeriodRequired && string.IsNullOrWhiteSpace(pricingInitRatePeriod))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_init_rate_period", "E2016", "Initial Rate Period is required when 'Pricing Interest Rate Type' is 3, 4, 5, or 6."));

                }

                // ✅ If not required but present, it must be blank
                if (!isRatePeriodRequired && !string.IsNullOrWhiteSpace(pricingInitRatePeriod))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_init_rate_period", "E2016", "Initial Rate Period must be left blank unless 'Pricing Interest Rate Type' is 3, 4, 5, or 6."));

                }

                // ✅ If present, ensure it is a valid whole number
                if (!string.IsNullOrWhiteSpace(pricingInitRatePeriod))
                {
                    if (!int.TryParse(pricingInitRatePeriod, out int initRatePeriod))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_init_rate_period", "E0400", "Initial Rate Period - Must be a whole number."));

                    }

                    // ✅ Ensure initial rate period is greater than 0
                    if (initRatePeriod <= 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_init_rate_period", "E0401", "Initial Rate Period - Must be greater than 0."));
                    }
                }
                ////////////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 22) // Assuming 'pricing_fixed_rate' is Column U (Index 20)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_fixed_rate", "", "Missing Pricing Fixed Rate - Column U must be present."));

                }

                string pricing_Interest_Rate_Type = fields[19]?.Trim() ?? ""; // Column S (pricing_interest_rate_type)
                string pricingFixedRate = fields[21]?.Trim() ?? ""; // Column U (pricing_fixed_rate)

                // ✅ Check if `pricing_interest_rate_type` is '2, 4, or 6' (conditionally required)
                bool isFixedRateRequired = new HashSet<string> { "2", "4", "6" }.Contains(pricing_Interest_Rate_Type);

                // ✅ If required but missing, throw an error
                if (isFixedRateRequired && string.IsNullOrWhiteSpace(pricingFixedRate))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_fixed_rate", "E2017", "Fixed Rate is required when 'Pricing Interest Rate Type' is 2, 4, or 6."));

                }

                // ✅ If not required but present, it must be blank
                if (!isFixedRateRequired && !string.IsNullOrWhiteSpace(pricingFixedRate))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_fixed_rate", "E2017", "Fixed Rate must be left blank unless 'Pricing Interest Rate Type' is 2, 4, or 6."));

                }

                // ✅ If present, ensure it is a valid numeric value
                if (!string.IsNullOrWhiteSpace(pricingFixedRate))
                {
                    if (!decimal.TryParse(pricingFixedRate, out decimal fixedRate))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_fixed_rate", "E0420", "Fixed Rate - Must be a numeric value."));

                    }

                    // ✅ Ensure fixed rate is generally greater than 0.1
                    if (fixedRate <= 0.1m)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_fixed_rate", "W0420", "Warning: Fixed Rate should generally be greater than 0.1."));
                    }
                }
                ////////////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 23) // Assuming 'pricing_fixed_rate' is Column U (Index 20)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_margin", "", "Missing Pricing adj Rate - Column U must be present."));

                }

                string pricing_Interest_RateType = fields[19]?.Trim() ?? ""; // Column S (pricing_interest_rate_type)
                string pricing_adj_margin = fields[22]?.Trim() ?? ""; // Column U (pricing_fixed_rate)

                // ✅ Check if `pricing_interest_rate_type` is '1, 3, or 5' (conditionally required)
                bool isFixedRate_Required = new HashSet<string> { "1", "3", "5" }.Contains(pricing_Interest_RateType);

                // ✅ If required but missing, throw an error
                if (isFixedRate_Required && string.IsNullOrWhiteSpace(pricing_adj_margin))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_margin", "E2018", "'adjustable rate is required when 'Pricing Interest Rate Type' is 1, 3, or 5."));

                }

                // ✅ If not required but present, it must be blank
                if (!isFixedRate_Required && !string.IsNullOrWhiteSpace(pricing_adj_margin))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_margin", "E2018", "adjustable rate must be left blank unless 'Pricing Interest Rate Type' is 1, 3, or 5."));

                }

                // ✅ If present, ensure it is a valid numeric value
                if (!string.IsNullOrWhiteSpace(pricing_adj_margin))
                {
                    if (!decimal.TryParse(pricing_adj_margin, out decimal fixedRate))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_margin", "E0440", "Adjustable Rate - Must be a numeric value."));

                    }

                    // ✅ Ensure fixed rate is generally greater than 0.1
                    if (fixedRate <= 0.1m)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_margin", "W0441", "Warning: Adjustable Rate should generally be greater than 0.1."));
                    }
                }
                ///////////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 24) // Assuming 'pricing_adj_index_name' is Column V (Index 21)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_name", "", "Missing Pricing Adjustment Index Name - Column V must be present."));

                }

                string pricingAdjIndexName = fields[23]?.Trim() ?? ""; // Column V (pricing_adj_index_name)

                // ✅ Required field: Ensure it is not empty (handles hidden spaces too)
                if (string.IsNullOrWhiteSpace(pricingAdjIndexName))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_name", "", "Pricing Adjustment Index Name is required and cannot be empty."));

                }

                // ✅ Allowed values
                HashSet<string> validAdjIndexNames = new HashSet<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "977", "999" };

                // ✅ Ensure the value is one of the allowed codes
                if (!validAdjIndexNames.Contains(pricingAdjIndexName))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_name", "E0460", "Pricing Adjustment Index Name - Must be 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 977, or 999."));
                }
                //////////////////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 25) // Assuming 'pricing_adj_index_name_ff' is Column W (Index 22)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_name_ff", "", "Missing field - Column W must be present."));

                }

                string pricingAdjIndex_Name = fields[23]?.Trim() ?? ""; // Column V (pricing_adj_index_name)
                string pricingAdjIndexNameFF = fields[24]?.Trim() ?? ""; // Column W (pricing_adj_index_name_ff)

                // ✅ Ensure text does not exceed 300 characters
                if (pricingAdjIndexNameFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_name_ff", "E0480", "input - Must not exceed 300 characters."));
                }

                // ✅ Check if `pricing_adj_index_name` contains `977`
                bool contains9777 = pricingAdjIndex_Name == "977";

                // ✅ If `pricing_adj_index_name` is `977`, `pricing_adj_index_name_ff` is required
                if (contains9777 && string.IsNullOrWhiteSpace(pricingAdjIndexNameFF))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_name_ff", "E2020", "Required field - Must specify other index name if 'adjustable rate transaction: index name' is 977."));
                }

                // ✅ If `pricing_adj_index_name` does not contain `977`, `pricing_adj_index_name_ff` must be blank
                if (!contains9777 && !string.IsNullOrWhiteSpace(pricingAdjIndexNameFF))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_name_ff", "E2020", "Invalid input - Must be left blank unless 'adjustable rate transaction: index name' is 977."));
                }
                //////////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 26) // Assuming 'pricing_adj_index_value' is Column X (Index 23)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_value", "", "Missing Pricing Adjustment Index Value - Column X must be present."));

                }

                string pricingAdj_Index_Name = fields[23]?.Trim() ?? ""; // Column S (pricing_interest_rate_type)
                string pricingAdjIndexValue = fields[25]?.Trim() ?? ""; // Column X (pricing_adj_index_value)

                // ✅ Check if `pricing_interest_rate_type` is '1' or '3' (conditionally required)
                bool isAdjIndexValueRequired = pricingAdj_Index_Name == "1" || pricingAdj_Index_Name == "3";

                // ✅ If required but missing, throw an error
                if (isAdjIndexValueRequired && string.IsNullOrWhiteSpace(pricingAdjIndexValue))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_value", "E2021", "Pricing Adjustment Index Value is required when 'Pricing Interest Index name' is 1 or 3."));

                }

                // ✅ If not required but present, it must be blank
                if (!isAdjIndexValueRequired && !string.IsNullOrWhiteSpace(pricingAdjIndexValue))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_value", "E2021", "Pricing Adjustment Index Value must be left blank unless 'Pricing Interest Index name' is 1 or 3."));

                }

                // ✅ If present, ensure it is a valid numeric value
                if (!string.IsNullOrWhiteSpace(pricingAdjIndexValue))
                {
                    if (!decimal.TryParse(pricingAdjIndexValue, out _))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_value", "E0500", "Pricing Adjustment Index Value - Must be a numeric value."));
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 27) // Assuming 'pricing_origination_charges' is Column Y (Index 24)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_origination_charges", "", "Missing Pricing Origination Charges - Column Y must be present."));

                }

                string actionTaken1 = fields[15]?.Trim() ?? ""; // Column O (action_taken)
                string pricingOriginationCharges = fields[26]?.Trim() ?? ""; // Column Y (pricing_origination_charges)

                // ✅ Check if `action_taken` is '1' or '2' (conditionally required)
                bool isOriginationChargesRequired = actionTaken1 == "1" || actionTaken1 == "2";

                // ✅ If required but missing, throw an error
                if (isOriginationChargesRequired && string.IsNullOrWhiteSpace(pricingOriginationCharges))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_origination_charges", "", "Pricing Origination Charges is required when 'Action Taken' is 1 or 2."));

                }

                // ✅ If not required but present, it must be blank
                if (!isOriginationChargesRequired && !string.IsNullOrWhiteSpace(pricingOriginationCharges))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_origination_charges", "", "Pricing Origination Charges must be left blank unless 'Action Taken' is 1 or 2."));

                }

                // ✅ If present, ensure it is a valid numeric value
                if (!string.IsNullOrWhiteSpace(pricingOriginationCharges))
                {
                    if (!decimal.TryParse(pricingOriginationCharges, out _))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_origination_charges", "E0520", "Pricing Origination Charges - Must be a numeric value."));
                    }
                }
                ////////////////////////////////////////////////////////////////////////////////////////
                // Ensure the row has enough columns to avoid index out-of-range errors
                if (fields.Length < 28) // Assuming 'pricing_broker_fees' is Column Z (Index 25)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_broker_fees", "", "Missing Pricing Broker Fees - Column Z must be present."));

                }

                string actionTaken2 = fields[15]?.Trim() ?? ""; // Column O (action_taken)
                string pricingBrokerFees = fields[27]?.Trim() ?? ""; // Column Z (pricing_broker_fees)

                // ✅ Check if `action_taken` is '1' or '2' (conditionally required)
                bool isBrokerFeesRequired = actionTaken2 == "1" || actionTaken2 == "2";

                // ✅ If required but missing, throw an error
                if (isBrokerFeesRequired && string.IsNullOrWhiteSpace(pricingBrokerFees))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_broker_fees", "", "Pricing Broker Fees is required when 'Action Taken' is 1 or 2."));

                }

                // ✅ If not required but present, it must be blank
                if (!isBrokerFeesRequired && !string.IsNullOrWhiteSpace(pricingBrokerFees))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_broker_fees", "", "Pricing Broker Fees must be left blank unless 'Action Taken' is 1 or 2."));

                }

                // ✅ If present, ensure it is a valid numeric value
                if (!string.IsNullOrWhiteSpace(pricingBrokerFees))
                {
                    if (!decimal.TryParse(pricingBrokerFees, out _))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_broker_fees", "E0540", "Pricing Broker Fees - Must be a numeric value."));
                    }
                }
                /////////////////////////////////////////////////////////////////////


                if (fields.Length < 29) // Index 28 requires Length of at least 29
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_initial_charges", "", "Missing Pricing Initial Charges - Column AA must be present."));
                }

                string actionTaken3 = fields[15]?.Trim() ?? "";              // Column O (action_taken)
                string pricingInitialCharges = fields[28]?.Trim() ?? "";       // Column AA (pricing_initial_charges)

                // ✅ Check if `action_taken` is '1' or '2' (conditionally required)
                bool isInitialChargesRequired = actionTaken3 == "1" || actionTaken3 == "2";

                // ✅ If required but missing, throw an error
                if (isInitialChargesRequired && string.IsNullOrWhiteSpace(pricingInitialCharges))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_initial_charges", "", "Pricing Initial Charges is required when 'Action Taken' is 1 or 2."));
                }

                // ✅ If not required but present, it must be blank
                if (!isInitialChargesRequired && !string.IsNullOrWhiteSpace(pricingInitialCharges))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_initial_charges", "", "Pricing Initial Charges must be left blank unless 'Action Taken' is 1 or 2."));
                }

                // ✅ If present, ensure it is a valid numeric value
                if (!string.IsNullOrWhiteSpace(pricingInitialCharges))
                {
                    if (!decimal.TryParse(pricingInitialCharges, out _))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_initial_charges", "E0560", "Pricing Initial Charges - Must be a numeric value."));
                    }
                }
                ///////////////////////////////////////////////////////////////////////////////
                // Ensure the fields array has enough columns for pricing_mca_addcost_flag (assumed at index 29)
                if (fields.Length < 30)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_mca_addcost_flag", "", "Missing Pricing MCA Addcost Flag - the column must be present."));
                }

                // Retrieve the value for pricing_mca_addcost_flag and trim whitespace
                string pricingMcaAddcostFlag = fields[29]?.Trim() ?? "";

                // ✅ This field is required for all application records.
                if (string.IsNullOrWhiteSpace(pricingMcaAddcostFlag))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_mca_addcost_flag", "", "Pricing MCA Addcost Flag is required."));
                }
                else
                {
                    // ✅ Validate that the field value equals "900" or "999"
                    if (pricingMcaAddcostFlag != "900" && pricingMcaAddcostFlag != "999")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_mca_addcost_flag", "E0580", "Pricing MCA Addcost Flag must equal 900 or 999."));
                    }
                }
                ///////////////////////////////////////////////////////////////////////////////
                // Ensure the fields array has enough columns for pricing_mca_addcost (assumed at index 30)
                if (fields.Length < 31)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_mca_addcost", "", "Missing Pricing MCA Addcost - the column must be present."));
                }

                // Retrieve the values for pricing_mca_addcost_flag and pricing_mca_addcost
                string pricingMcaAddcost_Flag = fields[29]?.Trim() ?? ""; // Column for pricing_mca_addcost_flag
                string pricingMcaAddcost = fields[30]?.Trim() ?? "";     // Column for pricing_mca_addcost

                // ✅ Check if `pricing_mca_addcost` is conditionally required
                // It is required only when pricing_mca_addcost_flag equals "900"
                bool isAddcostRequired = pricingMcaAddcost_Flag == "900";

                // ✅ If required but missing, throw an error
                if (isAddcostRequired && string.IsNullOrWhiteSpace(pricingMcaAddcost))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_mca_addcost", "E2023", "Pricing MCA Addcost is required when Pricing MCA Addcost Flag is 900."));
                }

                // ✅ If not required but present, it must be blank
                if (!isAddcostRequired && !string.IsNullOrWhiteSpace(pricingMcaAddcost))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_mca_addcost", "E2023", "Pricing MCA Addcost must be left blank unless Pricing MCA Addcost Flag is 900."));
                }

                // ✅ If present, ensure it is a valid numeric value
                if (!string.IsNullOrWhiteSpace(pricingMcaAddcost))
                {
                    if (!decimal.TryParse(pricingMcaAddcost, out _))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_mca_addcost", "E0600", "Pricing MCA Addcost - Must be a numeric value."));
                    }
                }
                ///////////////////////////////////////////////////////////////////////////////
                // Ensure the fields array has enough columns for pricing_prepenalty_allowed (assumed at index 31)
                if (fields.Length < 32)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_prepenalty_allowed", "", "Missing Pricing Prepenalty Allowed - the column must be present."));
                }

                string pricingPrepenaltyAllowed = fields[31]?.Trim() ?? "";

                // ✅ This field is required for all application records.
                if (string.IsNullOrWhiteSpace(pricingPrepenaltyAllowed))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_prepenalty_allowed", "", "Pricing Prepenalty Allowed is required."));
                }
                else
                {
                    // ✅ Validate that the field value equals "1", "2" or "999"
                    if (pricingPrepenaltyAllowed != "1" &&
                        pricingPrepenaltyAllowed != "2" &&
                        pricingPrepenaltyAllowed != "999")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_prepenalty_allowed", "E0620", "Pricing Prepenalty Allowed must equal 1, 2 or 999."));
                    }
                }
                ///////////////////////////////////////////////////////////////////////////////
                // Ensure the fields array has enough columns for pricing_prepenalty_exists (assumed at index 32)
                if (fields.Length < 33)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_prepenalty_exists", "", "Missing Pricing Prepenalty Exists - the column must be present."));
                }

                string pricingPrepenaltyExists = fields[32]?.Trim() ?? "";

                // ✅ This field is required for all application records.
                if (string.IsNullOrWhiteSpace(pricingPrepenaltyExists))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "pricing_prepenalty_exists", "", "Pricing Prepenalty Exists is required."));
                }
                else
                {
                    // ✅ Validate that the field value equals "1", "2" or "999"
                    if (pricingPrepenaltyExists != "1" &&
                        pricingPrepenaltyExists != "2" &&
                        pricingPrepenaltyExists != "999")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_prepenalty_exists", "E0640", "Pricing Prepenalty Exists must equal 1, 2 or 999."));
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////
                // Ensure the fields array has enough columns for census_tract_adr_type (assumed at index 33)
                if (fields.Length < 34)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "census_tract_adr_type", "", "Missing Census Tract ADR Type - the column must be present."));
                }

                string censusTractAdrType = fields[33]?.Trim() ?? "";

                // ✅ This field is required for all application records.
                if (string.IsNullOrWhiteSpace(censusTractAdrType))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "census_tract_adr_type", "", "Census Tract ADR Type is required."));
                }
                else
                {
                    // ✅ Validate that the field value equals "1", "2", "3", or "988"
                    if (censusTractAdrType != "1" &&
                        censusTractAdrType != "2" &&
                        censusTractAdrType != "3" &&
                        censusTractAdrType != "988")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "census_tract_adr_type", "E0660", "Census Tract ADR Type must equal 1, 2, 3, or 988."));
                    }
                }
                ///////////////////////////////////////////////////////////////////////////////
                // Ensure the fields array has enough columns for census_tract_number (assumed at index 34)
                if (fields.Length < 35)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "census_tract_number", "", "Missing Census Tract Number - the column must be present."));
                }

                // Retrieve values for census_tract_adr_type and census_tract_number
                string censusTractAdr_Type = fields[33]?.Trim() ?? "";  // Census Tract ADR Type (index 33)
                string censusTractNumber = fields[34]?.Trim() ?? "";   // Census Tract Number (index 34)

                // Determine conditional requirement based on census_tract_adr_type:
                // - Required if the code is "1", "2", or "3".
                // - Must be left blank if the code is "988".
                bool isCensusTractNumberRequired = censusTractAdr_Type == "1" || censusTractAdr_Type == "2" || censusTractAdr_Type == "3";
                bool mustBeBlank = censusTractAdr_Type == "988";

                // If required but missing, add an error.
                if (isCensusTractNumberRequired && string.IsNullOrWhiteSpace(censusTractNumber))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "census_tract_number", "E2024", "Census Tract Number is required when Census Tract ADR Type is 1, 2, or 3."));
                }

                // If the ADR type is 988, the field must be blank.
                if (mustBeBlank && !string.IsNullOrWhiteSpace(censusTractNumber))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "census_tract_number", "E2024", "Census Tract Number must be left blank when Census Tract ADR Type is 988."));
                }

                // When a value is provided, validate that it is a GEOID with exactly 11 digits.
                if (!string.IsNullOrWhiteSpace(censusTractNumber))
                {
                    // Check for an exact width of 11 characters.
                    if (censusTractNumber.Length != 11)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "census_tract_number", "E0680", "Census Tract Number must be exactly 11 characters long."));
                    }

                    // Ensure that every character is a digit.
                    if (!censusTractNumber.All(char.IsDigit))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "census_tract_number", "", "Census Tract Number must contain only digits."));
                    }

                    // Optionally, validate that the value is a valid census tract GEOID as defined by the U.S. Census Bureau.
                    // This could involve additional business logic or a lookup function. For example:
                    // if (!IsValidCensusTractGEOID(censusTractNumber))
                    // {
                    //     validationErrors.Add(new ValidationError(rowNumber, "census_tract_number", "Census Tract Number is not a valid Census Tract GEOID."));
                    // }
                }

                // Ensure the fields array has enough columns for gross_annual_revenue_flag (assumed at index 35)
                if (fields.Length < 36)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "gross_annual_revenue_flag", "", "Missing Gross Annual Revenue Flag - the column must be present."));
                }

                string grossAnnualRevenueFlag = fields[35]?.Trim() ?? "";

                // ✅ This field is required for all application records.
                if (string.IsNullOrWhiteSpace(grossAnnualRevenueFlag))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "gross_annual_revenue_flag", "", "Gross Annual Revenue Flag is required."));
                }
                else
                {
                    // ✅ Validate that the field value equals "900" or "988"
                    if (grossAnnualRevenueFlag != "900" && grossAnnualRevenueFlag != "988")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "gross_annual_revenue_flag", "E0700", "Gross Annual Revenue Flag must equal 900 or 988."));
                    }
                }
                // Ensure the fields array has enough columns for gross_annual_revenue (assumed at index 36)
                if (fields.Length < 37)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "gross_annual_revenue", "", "Missing Gross Annual Revenue - the column must be present."));
                }

                // Retrieve values for gross_annual_revenue_flag and gross_annual_revenue
                string grossAnnualRevenue_Flag = fields[35]?.Trim() ?? "";  // Already validated in a previous step
                string grossAnnualRevenue = fields[36]?.Trim() ?? "";

                // Determine the conditional requirement:
                // - Gross Annual Revenue is required if gross_annual_revenue_flag equals "900"
                // - Otherwise, the field must be left blank.
                bool isRevenueRequired = grossAnnualRevenue_Flag == "900";

                // If the field is required but missing, add an error.
                if (isRevenueRequired && string.IsNullOrWhiteSpace(grossAnnualRevenue))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "gross_annual_revenue", "E2025", "Gross Annual Revenue is required when Gross Annual Revenue Flag is 900."));
                }

                // If the field is not required but a value is provided, add an error.
                if (!isRevenueRequired && !string.IsNullOrWhiteSpace(grossAnnualRevenue))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "gross_annual_revenue", "E2025", "Gross Annual Revenue must be left blank unless Gross Annual Revenue Flag is 900."));
                }

                // When a value is provided, ensure it is a valid numeric value.
                if (!string.IsNullOrWhiteSpace(grossAnnualRevenue))
                {
                    if (!decimal.TryParse(grossAnnualRevenue, out _))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "gross_annual_revenue", "E0720", "Gross Annual Revenue - Must be a numeric value."));
                    }
                }

                // Ensure the fields array has enough columns for naics_code_flag (assumed at index 37)
                if (fields.Length < 38)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "naics_code_flag", "", "Missing NAICS Code Flag - the column must be present."));
                }

                string naicsCodeFlag = fields[37]?.Trim() ?? "";

                // ✅ This field is required for all application records.
                if (string.IsNullOrWhiteSpace(naicsCodeFlag))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "naics_code_flag", "", "NAICS Code Flag is required."));
                }
                else
                {
                    // ✅ Validate that the field value equals "900" or "988"
                    if (naicsCodeFlag != "900" && naicsCodeFlag != "988")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "naics_code_flag", "E0740", "NAICS Code Flag must equal 900 or 988."));
                    }
                }

                // Ensure the fields array has enough columns for naics_code (assumed at index 38)
                if (fields.Length < 39)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "naics_code", "", "Missing NAICS Code - the column must be present."));
                }

                // Retrieve the values for naics_code_flag and naics_code
                string naicsCode_Flag = fields[37]?.Trim() ?? ""; // NAICS Code Flag (index 37)
                string naicsCode = fields[38]?.Trim() ?? ""; // NAICS Code (index 38)

                // Determine whether the NAICS Code is conditionally required:
                // - Required if naics_code_flag equals "900"
                // - Must be left blank if naics_code_flag is not "900"
                bool isNaicsCodeRequired = naicsCode_Flag == "900";

                // If the field is required but missing, add an error.
                if (isNaicsCodeRequired && string.IsNullOrWhiteSpace(naicsCode))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "naics_code", "E2026", "NAICS Code is required when NAICS Code Flag is 900."));
                }

                // If the field is not required but a value is provided, add an error.
                if (!isNaicsCodeRequired && !string.IsNullOrWhiteSpace(naicsCode))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "naics_code", "E2026", "NAICS Code must be left blank unless NAICS Code Flag is 900."));
                }

                // When a value is provided, validate that it meets the following criteria.
                if (!string.IsNullOrWhiteSpace(naicsCode))
                {
                    // Check that the code is exactly 3 characters long.
                    if (naicsCode.Length != 3)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "naics_code", "E0760", "NAICS Code must be exactly 3 characters long."));
                    }

                    // Check that every character is numeric.
                    if (!naicsCode.All(char.IsDigit))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "naics_code", "E0761", " NAICS Code must contain only numeric characters."));
                    }

                    // Optionally, validate that the NAICS code is a valid code per industry standards.
                    // For example, if you have a method IsValidNaicsCode(string code):
                    // if (!IsValidNaicsCode(naicsCode))
                    // {
                    //     validationErrors.Add(new ValidationError(rowNumber, "naics_code", "NAICS Code is not a valid code."));
                    // }
                }

                // Ensure the fields array has enough columns for number_of_workers (assumed at index 39)
                if (fields.Length < 40)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "number_of_workers", "", "Missing Number of Workers - the column must be present."));
                }

                string numberOfWorkers = fields[39]?.Trim() ?? "";

                // ✅ This field is required for all application records.
                if (string.IsNullOrWhiteSpace(numberOfWorkers))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "number_of_workers", "", "Number of Workers is required."));
                }
                else
                {
                    // Allowed values: "1", "2", "3", "4", "5", "6", "7", "8", "9", or "988"
                    string[] validValues = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "988" };

                    // Validate that the field value equals one of the allowed values.
                    if (!validValues.Contains(numberOfWorkers))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "number_of_workers", "E0780", " Number of Workers must equal 1, 2, 3, 4, 5, 6, 7, 8, 9, or 988."));
                    }
                }

                // Ensure the fields array has enough columns for time_in_business_type (assumed at index 40)
                if (fields.Length < 41)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "time_in_business_type", "", "Missing Time in Business Type - the column must be present."));
                }

                string timeInBusinessType = fields[40]?.Trim() ?? "";

                // ✅ This field is required for all application records.
                if (string.IsNullOrWhiteSpace(timeInBusinessType))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "time_in_business_type", "", "Time in Business Type is required."));
                }
                else
                {
                    // Allowed values: "1", "2", "3", or "988"
                    string[] validValues = { "1", "2", "3", "988" };

                    // Validate that the field value equals one of the allowed values.
                    if (!validValues.Contains(timeInBusinessType))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "time_in_business_type", "E0800", "Time in Business Type must equal 1, 2, 3, or 988."));
                    }
                }

                // Ensure the fields array has enough columns for time_in_business (assumed at index 41)
                if (fields.Length < 42)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "time_in_business", "", "Missing Time in Business - the column must be present."));
                }

                string timeInBusiness_Type = fields[40]?.Trim() ?? ""; // Time in Business Type (index 40)
                string timeInBusiness = fields[41]?.Trim() ?? ""; // Time in Business (index 41)

                // Determine if Time in Business is conditionally required:
                // - It is required if Time in Business Type equals "1".
                // - It must be left blank if Time in Business Type is not "1".
                bool isTimeInBusinessRequired = timeInBusiness_Type == "1";

                // If required but missing, add an error.
                if (isTimeInBusinessRequired && string.IsNullOrWhiteSpace(timeInBusiness))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "time_in_business", "E2027", "Time in Business is required when Time in Business Type is 1."));
                }

                // If not required but a value is provided, add an error.
                if (!isTimeInBusinessRequired && !string.IsNullOrWhiteSpace(timeInBusiness))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "time_in_business", "E2027", "Time in Business must be left blank unless Time in Business Type is 1."));
                }

                // When a value is provided, validate that it is a whole number and >= 0.
                if (!string.IsNullOrWhiteSpace(timeInBusiness))
                {
                    // Try parsing the field as an integer.
                    if (!int.TryParse(timeInBusiness, out int parsedTime))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "time_in_business", "E0820", "Time in Business - must be a whole number."));
                    }
                    else if (parsedTime < 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "time_in_business", "E0821", " Time in Business must be greater than or equal to 0."));
                    }
                }
                // Ensure the fields array has enough columns for business_ownership_status (assumed at index 42)
                if (fields.Length < 43)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "business_ownership_status", "", "Missing Business Ownership Status - the column must be present."));
                }

                string businessOwnershipStatus = fields[42]?.Trim() ?? "";

                // ✅ This field is required for all application records.
                if (string.IsNullOrWhiteSpace(businessOwnershipStatus))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "business_ownership_status", "", "Business Ownership Status is required."));
                }
                else
                {
                    // Split the field into individual codes using a semicolon as the delimiter.
                    var codes = businessOwnershipStatus
                                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(code => code.Trim())
                                    .ToList();

                    // Validation: Must contain at least one value.
                    if (codes.Count == 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "business_ownership_status", "E0841", "Business Ownership Status must contain at least one value."));
                    }

                    // Validation: Should not contain duplicated values.
                    if (codes.Count != codes.Distinct().Count())
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "business_ownership_status", "W0842", "Business Ownership Status should not contain duplicate values."));
                    }

                    // Allowed values for each code.
                    string[] allowedCodes = new[] { "1", "2", "3", "955", "966", "988" };

                    // Validation: Each value must equal one of the allowed codes.
                    foreach (var code in codes)
                    {
                        if (!allowedCodes.Contains(code))
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "business_ownership_status", "E0840", "Invalid Business Ownership Status code: {code}. Allowed values are 1, 2, 3, 955, 966, or 988."));
                        }
                    }

                    // Validation: When code 966 or 988 is reported, no other codes should be present.
                    if ((codes.Contains("966") || codes.Contains("988")) && codes.Count > 1)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "business_ownership_status", "W0843", "When code 966 or 988 is reported, no other codes should be included."));
                    }
                }

                // Ensure the fields array has enough columns for num_principal_owners_flag (assumed at index 43)
                if (fields.Length < 44)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "num_principal_owners_flag", "", "Missing Num Principal Owners Flag - the column must be present."));
                }

                string numPrincipalOwnersFlag = fields[43]?.Trim() ?? "";

                // ✅ This field is required for all application records.
                if (string.IsNullOrWhiteSpace(numPrincipalOwnersFlag))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "num_principal_owners_flag", "", "Num Principal Owners Flag is required."));
                }
                else
                {
                    // ✅ Validate that the field value equals "900" or "988"
                    if (numPrincipalOwnersFlag != "900" && numPrincipalOwnersFlag != "988")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "num_principal_owners_flag", "E0860", "Num Principal Owners Flag must equal 900 or 988."));
                    }
                }

                // Ensure the fields array has enough columns for num_principal_owners (assumed at index 44)
                if (fields.Length < 45)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "num_principal_owners", "", "Missing Num Principal Owners - the column must be present."));
                }

                string numPrincipalOwners_Flag = fields[43]?.Trim() ?? ""; // Retrieved from index 43
                string numPrincipalOwners = fields[44]?.Trim() ?? ""; // Retrieved from index 44

                // Determine if Num Principal Owners is conditionally required:
                // - It is required if num_principal_owners_flag equals "900".
                // - It must be left blank if num_principal_owners_flag is not "900".
                bool isNumPrincipalOwnersRequired = numPrincipalOwners_Flag == "900";

                // If required but missing, add an error.
                if (isNumPrincipalOwnersRequired && string.IsNullOrWhiteSpace(numPrincipalOwners))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "num_principal_owners", "E2028", "Num Principal Owners is required when Num Principal Owners Flag is 900."));
                }

                // If not required but a value is provided, add an error.
                if (!isNumPrincipalOwnersRequired && !string.IsNullOrWhiteSpace(numPrincipalOwners))
                {
                    validationErrors.Add(new ValidationError(rowNumber, "num_principal_owners", "E2028", "Num Principal Owners must be left blank unless Num Principal Owners Flag is 900."));
                }

                // When a value is provided, validate that it equals one of the allowed values: 0, 1, 2, 3, or 4.
                if (!string.IsNullOrWhiteSpace(numPrincipalOwners))
                {
                    string[] validValues = { "0", "1", "2", "3", "4" };
                    if (!validValues.Contains(numPrincipalOwners))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "num_principal_owners", "E0880", "Num Principal Owners must equal 0, 1, 2, 3, or 4."));
                    }
                }

                // Ensure the fields array has enough columns for po_1_ethnicity (assumed at index 45)
                if (fields.Length < 46)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity", "", "Missing PO 1 Ethnicity - the column must be present."));
                }

                string numPrincipal_Owners = fields[44]?.Trim() ?? ""; // Retrieved from index 44
                string po1Ethnicity = fields[45]?.Trim() ?? ""; // Retrieved from index 45

                // Check the conditional requirement based on num_principal_owners:
                // - If there is exactly 1 principal owner, then po_1_ethnicity is required.
                // - If there are not exactly 1 principal owner (or no principal owners), the field should be left blank.
                if (numPrincipal_Owners == "1")
                {
                    // Field is required.
                    if (string.IsNullOrWhiteSpace(po1Ethnicity))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity", "", "PO 1 Ethnicity is required when there is exactly 1 principal owner."));
                    }
                }
                else
                {
                    // Field should be blank.
                    if (!string.IsNullOrWhiteSpace(po1Ethnicity))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity", "", "PO 1 Ethnicity must be left blank if there are not exactly 1 principal owner."));
                    }
                }

                // If a value is provided (and it is applicable), perform further validations.
                if (!string.IsNullOrWhiteSpace(po1Ethnicity))
                {
                    // Split the multiple responses by semicolon and trim each code.
                    var codes = po1Ethnicity.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(code => code.Trim())
                                            .ToList();

                    // Validation: Must contain at least one value.
                    if (codes.Count == 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity", "", "PO 1 Ethnicity must contain at least one value."));
                    }

                    // Validation: Should not contain duplicated values.
                    if (codes.Count != codes.Distinct().Count())
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity", "W0901", "PO 1 Ethnicity should not contain duplicate values."));
                    }

                    // Allowed values for each response.
                    string[] allowedValues = { "1", "11", "12", "13", "14", "2", "966", "977", "988" };

                    // Validate that each code is allowed.
                    foreach (var code in codes)
                    {
                        if (!allowedValues.Contains(code))
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity", "E0900", "ethnicity code: {code}. Allowed values are 1, 11, 12, 13, 14, 2, 966, 977, or 988."));
                        }
                    }

                    // Validation: When code 966 or 988 is reported, no other codes should be present.
                    if ((codes.Contains("966") || codes.Contains("988")) && codes.Count > 1)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity", "W0902", "When code 966 or 988 is reported, no other values should be included."));
                    }
                }

                // Ensure the fields array has enough columns for po_1_ethnicity_ff (assumed at index 46)
                if (fields.Length < 47)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity_ff", "", "Missing PO 1 Ethnicity Free Form - the column must be present."));
                }

                string po1_Ethnicity = fields[45]?.Trim() ?? ""; // PO 1 Ethnicity from index 45
                string po1EthnicityFF = fields[46]?.Trim() ?? ""; // PO 1 Ethnicity Free Form from index 46

                // Determine whether the free form field is required based on the presence of code "977" in po_1_ethnicity.
                bool requiresFreeForm = false;
                if (!string.IsNullOrWhiteSpace(po1_Ethnicity))
                {
                    // Split the value into individual codes (using semicolon as the delimiter) and trim each code.
                    var ethnicityCodes = po1_Ethnicity
                                            .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(code => code.Trim())
                                            .ToList();

                    // The free form field is required if one of the codes equals "977".
                    requiresFreeForm = ethnicityCodes.Contains("977");
                }

                if (requiresFreeForm)
                {
                    // When code 977 is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po1EthnicityFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity_ff", "E2040", "PO 1 Ethnicity Free Form is required when code 977 is reported in PO 1 Ethnicity."));
                    }
                }
                else
                {
                    // When code 977 is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po1EthnicityFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity_ff", "E2040", "PO 1 Ethnicity Free Form must be left blank when code 977 is not reported in PO 1 Ethnicity."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po1EthnicityFF) && po1EthnicityFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_ethnicity_ff", "E0920", "PO 1 Ethnicity Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race (assumed at index 47)
                if (fields.Length < 48)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_race", "", "Missing PO 1 Race - the column must be present."));
                }

                string num_PrincipalOwners = fields[44]?.Trim() ?? ""; // Principal owners count from index 44
                string po1Race = fields[47]?.Trim() ?? ""; // PO 1 Race from index 47

                // Conditional Requirement:
                // - If there is exactly 1 principal owner, then po_1_race is required.
                // - Otherwise, the field should be left blank.
                if (num_PrincipalOwners == "1")
                {
                    if (string.IsNullOrWhiteSpace(po1Race))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race", "", "PO 1 Race is required when there is exactly 1 principal owner."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po1Race))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race", "", "PO 1 Race must be left blank if there are not exactly 1 principal owner."));
                    }
                }

                // When a value is provided, perform further validations.
                if (!string.IsNullOrWhiteSpace(po1Race))
                {
                    // Split the field into individual race codes using semicolons as the delimiter.
                    var raceCodes = po1Race
                                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(code => code.Trim())
                                        .ToList();

                    // Validation: Must contain at least one value.
                    if (raceCodes.Count == 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race", "", "PO 1 Race must contain at least one value."));
                    }

                    // Validation: Should not contain duplicated values.
                    if (raceCodes.Count != raceCodes.Distinct().Count())
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race", "W0941", "PO 1 Race should not contain duplicate values."));
                    }

                    // Allowed race codes.
                    string[] allowedCodes = {
                                            "1", "2",
                                           "21", "22", "23", "24", "25", "26", "27",
                                                      "3", "31", "32", "33", "34", "35", "36", "37",
                                                        "4", "41", "42", "43", "44",
                                                                   "5",
                                                 "966", "971", "972", "973", "974",
                                                         "988"
                                                      };

                    // Validate that each code is allowed.
                    foreach (var code in raceCodes)
                    {
                        if (!allowedCodes.Contains(code))
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_1_race", "E0940", "PO 1 Race code: {code}. Allowed values are 1, 2, 21, 22, 23, 24, 25, 26, 27, 3, 31, 32, 33, 34, 35, 36, 37, 4, 41, 42, 43, 44, 5, 966, 971, 972, 973, 974, or 988."));
                        }
                    }

                    // Special Rule: When code 966 or 988 is reported, no other codes should be present.
                    if ((raceCodes.Contains("966") || raceCodes.Contains("988")) && raceCodes.Count > 1)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race", "W0942", "When code 966 or 988 is reported in PO 1 Race, no other codes should be included."));
                    }
                }

                // Ensure the fields array has enough columns for po_1_race_anai_ff (assumed at index 48)
                if (fields.Length < 49)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_race_anai_ff", "", "Missing PO 1 Race ANAI Free Form - the column must be present."));
                }

                string po1_Race = fields[47]?.Trim() ?? "";  // PO 1 Race from index 47
                string po1RaceAnaiFF = fields[48]?.Trim() ?? ""; // PO 1 Race ANAI Free Form from index 48

                // Determine whether the free form field is required based on the presence of code "971" in po_1_race.
                bool requiresAnaiFF = false;
                if (!string.IsNullOrWhiteSpace(po1_Race))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po1_Race.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(code => code.Trim())
                                           .ToList();

                    // The free form field is required if one of the codes equals "971".
                    requiresAnaiFF = raceCodes.Contains("971");
                }

                if (requiresAnaiFF)
                {
                    // When code "971" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po1RaceAnaiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race_anai_ff", "E2060", "PO 1 Race ANAI Free Form is required when code 971 is reported in PO 1 Race."));
                    }
                }
                else
                {
                    // When code "971" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po1RaceAnaiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race_anai_ff", "E2060", "PO 1 Race ANAI Free Form must be left blank when code 971 is not reported in PO 1 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po1RaceAnaiFF) && po1RaceAnaiFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_race_anai_ff", "E0960", "PO 1 Race ANAI Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_asian_ff (assumed at index 49)
                if (fields.Length < 50)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_race_asian_ff", "", "Missing PO 1 Race Asian Free Form - the column must be present."));
                }

                string po1Race1 = fields[47]?.Trim() ?? "";  // PO 1 Race from index 47
                string po1RaceAsianFF = fields[49]?.Trim() ?? "";  // PO 1 Race Asian Free Form from index 49

                // Determine whether the free form field is required based on the presence of code "972" in po_1_race.
                bool requiresAsianFF = false;
                if (!string.IsNullOrWhiteSpace(po1Race1))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po1Race1.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(code => code.Trim())
                                           .ToList();

                    // The free form field is required if one of the codes equals "972".
                    requiresAsianFF = raceCodes.Contains("972");
                }

                if (requiresAsianFF)
                {
                    // When code "972" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po1RaceAsianFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race_asian_ff", "E2080", "PO 1 Race Asian Free Form is required when code 972 is reported in PO 1 Race."));
                    }
                }
                else
                {
                    // When code "972" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po1RaceAsianFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race_asian_ff", "E2080", "PO 1 Race Asian Free Form must be left blank when code 972 is not reported in PO 1 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po1RaceAsianFF) && po1RaceAsianFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_race_asian_ff", "E0980", "PO 1 Race Asian Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_baa_ff (assumed at index 50)
                if (fields.Length < 51)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_race_baa_ff", "", "Missing PO 1 Race BAA Free Form - the column must be present."));
                }

                string po1Race2 = fields[47]?.Trim() ?? "";  // PO 1 Race from index 47
                string po1RaceBaaFF = fields[50]?.Trim() ?? "";  // PO 1 Race BAA Free Form from index 50

                // Determine whether the free form field is required based on the presence of code "973" in po_1_race.
                bool requiresBaaFF = false;
                if (!string.IsNullOrWhiteSpace(po1Race2))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po1Race2
                                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(code => code.Trim())
                                    .ToList();

                    // The free form field is required if one of the codes equals "973".
                    requiresBaaFF = raceCodes.Contains("973");
                }

                if (requiresBaaFF)
                {
                    // When code "973" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po1RaceBaaFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race_baa_ff", "E2100", "PO 1 Race BAA Free Form is required when code 973 is reported in PO 1 Race."));
                    }
                }
                else
                {
                    // When code "973" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po1RaceBaaFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race_baa_ff", "E2100", "PO 1 Race BAA Free Form must be left blank when code 973 is not reported in PO 1 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po1RaceBaaFF) && po1RaceBaaFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_race_baa_ff", "E1000", "PO 1 Race BAA Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_pi_ff (assumed at index 51)
                if (fields.Length < 52)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_race_pi_ff", "", "Missing PO 1 Race PI Free Form - the column must be present."));
                }

                string po1Race3 = fields[47]?.Trim() ?? "";  // PO 1 Race from index 47
                string po1RacePiFF = fields[51]?.Trim() ?? "";  // PO 1 Race PI Free Form from index 51

                // Determine whether the free form field is required based on the presence of code "974" in po_1_race.
                bool requiresPiFF = false;
                if (!string.IsNullOrWhiteSpace(po1Race3))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po1Race3
                                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(code => code.Trim())
                                        .ToList();

                    // The free form field is required if one of the codes equals "974".
                    requiresPiFF = raceCodes.Contains("974");
                }

                if (requiresPiFF)
                {
                    // When code "974" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po1RacePiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race_pi_ff", "E2120", "PO 1 Race PI Free Form is required when code 974 is reported in PO 1 Race."));
                    }
                }
                else
                {
                    // When code "974" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po1RacePiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_race_pi_ff", "E2120", "PO 1 Race PI Free Form must be left blank when code 974 is not reported in PO 1 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po1RacePiFF) && po1RacePiFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_race_pi_ff", "E1020", "PO 1 Race PI Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_gender_flag (assumed at index 52)
                if (fields.Length < 53)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_gender_flag", "", "Missing PO 1 Gender Flag - the column must be present."));
                }

                // Retrieve the number of principal owners and the PO 1 Gender Flag
                string numPrincipalOwners2 = fields[44]?.Trim() ?? "";
                string po1GenderFlag = fields[52]?.Trim() ?? "";

                // Conditional Requirement:
                // - If there is at least one principal owner (numPrincipalOwners is not "0" or not blank), 
                //   then po_1_gender_flag is required and must equal "1", "966", or "988".
                // - If there are no principal owners (numPrincipalOwners equals "0" or is blank), 
                //   then po_1_gender_flag should be left blank.
                if (numPrincipalOwners2 == "1")
                {
                    // No principal owners; field should be left blank.

                    if (string.IsNullOrWhiteSpace(po1GenderFlag))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_gender_flag", "", "PO 1 Gender Flag is required when there is at least one principal owner."));
                    }
                }
                else
                {
                    // At least one principal owner; field is required.
                    if (!string.IsNullOrWhiteSpace(po1GenderFlag))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_gender_flag", "", "PO 1 Gender Flag must be left blank if there are no principal owners."));
                    }
                    else
                    {
                        // Validate that the flag equals one of the allowed values.
                        if (po1GenderFlag != "1" && po1GenderFlag != "966" && po1GenderFlag != "988")
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_1_gender_flag", "E1040", "PO 1 Gender Flag must equal 1, 966, or 988."));
                        }
                    }
                }


                // Ensure the fields array has enough columns for po_1_gender_ff (assumed at index 53)
                if (fields.Length < 54)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_gender_ff", "", "Missing PO 1 Gender Free Form - the column must be present."));
                }

                string po1GenderFlag1 = fields[52]?.Trim() ?? "";  // PO 1 Gender Flag from index 52
                string po1GenderFF = fields[53]?.Trim() ?? "";    // PO 1 Gender Free Form from index 53

                // Conditional Requirement:
                // - If po_1_gender_flag equals "1", then po_1_gender_ff is required.
                // - If po_1_gender_flag is not "1", then po_1_gender_ff must be left blank.
                if (po1GenderFlag1 == "1")
                {
                    if (string.IsNullOrWhiteSpace(po1GenderFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_gender_ff", "E2140", "PO 1 Gender Free Form is required when PO 1 Gender Flag is 1."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po1GenderFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_1_gender_ff", "E2140", "PO 1 Gender Free Form must be left blank unless PO 1 Gender Flag is 1."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po1GenderFF) && po1GenderFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_1_gender_ff", "E1060", "PO 1 Gender Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_2_ethnicity (assumed at index 54)
                if (fields.Length < 55)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity", "", "Missing PO 2 Ethnicity - the column must be present."));
                }

                string numPrincipalOwners3 = fields[44]?.Trim() ?? "";
                string po2Ethnicity = fields[54]?.Trim() ?? "";

                // Conditional Requirement:
                // - If num_principal_owners equals "2", then po_2_ethnicity is required.
                // - If num_principal_owners is not "2" (i.e. fewer than two principal owners), then po_2_ethnicity must be left blank.
                if (numPrincipalOwners3 == "2")
                {
                    if (string.IsNullOrWhiteSpace(po2Ethnicity))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity", "", "PO 2 Ethnicity is required when there are exactly 2 principal owners."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po2Ethnicity))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity", "", "PO 2 Ethnicity must be left blank if there are fewer than 2 principal owners."));
                    }
                }

                // If a value is provided, perform further validations.
                if (!string.IsNullOrWhiteSpace(po2Ethnicity))
                {
                    // Split the field into individual codes using semicolons as the delimiter.
                    var codes = po2Ethnicity
                                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(code => code.Trim())
                                    .ToList();

                    // Validation: Must contain at least one value.
                    if (codes.Count == 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity", "", "PO 2 Ethnicity must contain at least one value."));
                    }

                    // Validation: Should not contain duplicated values.
                    if (codes.Count != codes.Distinct().Count())
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity", "W1081", "PO 2 Ethnicity must not contain duplicate values."));
                    }

                    // Allowed values for each code.
                    string[] allowedValues = { "1", "11", "12", "13", "14", "2", "966", "977", "988" };

                    // Validate that each code is allowed.
                    foreach (var code in codes)
                    {
                        if (!allowedValues.Contains(code))
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity", "E1080", $"Invalid PO 2 Ethnicity code: {code}. Allowed values are 1, 11, 12, 13, 14, 2, 966, 977, or 988."));
                        }
                    }

                    // Validation: When code 966 or 988 is reported, no other codes should be present.
                    if ((codes.Contains("966") || codes.Contains("988")) && codes.Count > 1)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity", "W1082", "When code 966 or 988 is reported in PO 2 Ethnicity, no other codes should be included."));
                    }
                }

                // Ensure the fields array has enough columns for po_1_ethnicity_ff (assumed at index 46)
                if (fields.Length < 56)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity_ff", "", "Missing PO 2 Ethnicity Free Form - the column must be present."));
                }

                string po2_Ethnicity = fields[54]?.Trim() ?? ""; // PO 2 Ethnicity from index 45
                string po2EthnicityFF = fields[55]?.Trim() ?? ""; // PO 2 Ethnicity Free Form from index 46

                // Determine whether the free form field is required based on the presence of code "977" in po_1_ethnicity.
                bool requiresFreeForm2 = false;
                if (!string.IsNullOrWhiteSpace(po2_Ethnicity))
                {
                    // Split the value into individual codes (using semicolon as the delimiter) and trim each code.
                    var ethnicityCodes = po2_Ethnicity
                                            .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(code => code.Trim())
                                            .ToList();

                    // The free form field is required if one of the codes equals "977".
                    requiresFreeForm2 = ethnicityCodes.Contains("977");
                }

                if (requiresFreeForm2)
                {
                    // When code 977 is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po2EthnicityFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity_ff", "E2041", "PO 2 Ethnicity Free Form is required when code 977 is reported in PO 2 Ethnicity."));
                    }
                }
                else
                {
                    // When code 977 is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po2EthnicityFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity_ff", "E2041", "PO 2 Ethnicity Free Form must be left blank when code 977 is not reported in PO 2 Ethnicity."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po2EthnicityFF) && po2EthnicityFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_ethnicity_ff", "E1100", "PO 2 Ethnicity Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race (assumed at index 47)
                if (fields.Length < 57)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_race", "", "Missing PO 2 Race - the column must be present."));
                }

                string num_PrincipalOwners2 = fields[44]?.Trim() ?? ""; // Principal owners count from index 44
                string po2Race = fields[56]?.Trim() ?? ""; // PO 1 Race from index 47

                // Conditional Requirement:
                // - If there is exactly 1 principal owner, then po_1_race is required.
                // - Otherwise, the field should be left blank.
                if (num_PrincipalOwners2 == "2")
                {
                    if (string.IsNullOrWhiteSpace(po2Race))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race", "", "PO 2 Race is required when there is exactly 2 principal owner."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po2Race))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race", "", "PO 2 Race must be left blank if there are not exactly 2 principal owner."));
                    }
                }

                // When a value is provided, perform further validations.
                if (!string.IsNullOrWhiteSpace(po2Race))
                {
                    // Split the field into individual race codes using semicolons as the delimiter.
                    var raceCodes = po2Race
                                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(code => code.Trim())
                                        .ToList();

                    // Validation: Must contain at least one value.
                    if (raceCodes.Count == 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race", "", "PO 2 Race must contain at least one value."));
                    }

                    // Validation: Should not contain duplicated values.
                    if (raceCodes.Count != raceCodes.Distinct().Count())
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race", "W1121", "PO 2 Race should not contain duplicate values."));
                    }

                    // Allowed race codes.
                    string[] allowedCodes = {
                                            "1", "2",
                                           "21", "22", "23", "24", "25", "26", "27",
                                                      "3", "31", "32", "33", "34", "35", "36", "37",
                                                        "4", "41", "42", "43", "44",
                                                                   "5",
                                                 "966", "971", "972", "973", "974",
                                                         "988"
                                                      };

                    // Validate that each code is allowed.
                    foreach (var code in raceCodes)
                    {
                        if (!allowedCodes.Contains(code))
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_2_race", "E1120", $"Invalid PO 2 Race code: {code}. Allowed values are 1, 2, 21, 22, 23, 24, 25, 26, 27, 3, 31, 32, 33, 34, 35, 36, 37, 4, 41, 42, 43, 44, 5, 966, 971, 972, 973, 974, or 988."));
                        }
                    }

                    // Special Rule: When code 966 or 988 is reported, no other codes should be present.
                    if ((raceCodes.Contains("966") || raceCodes.Contains("988")) && raceCodes.Count > 1)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race", "W1122", "When code 966 or 988 is reported in PO 2 Race, no other codes should be included."));
                    }
                }

                // Ensure the fields array has enough columns for po_1_race_anai_ff (assumed at index 48)
                if (fields.Length < 58)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_race_anai_ff", "", "Missing PO 2 Race ANAI Free Form - the column must be present."));
                }

                string po2_Race = fields[56]?.Trim() ?? "";  // PO 1 Race from index 47
                string po2RaceAnaiFF = fields[57]?.Trim() ?? ""; // PO 1 Race ANAI Free Form from index 48

                // Determine whether the free form field is required based on the presence of code "971" in po_1_race.
                bool requiresAnaiFFF = false;
                if (!string.IsNullOrWhiteSpace(po2_Race))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po2_Race.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(code => code.Trim())
                                           .ToList();

                    // The free form field is required if one of the codes equals "971".
                    requiresAnaiFFF = raceCodes.Contains("971");
                }

                if (requiresAnaiFFF)
                {
                    // When code "971" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po2RaceAnaiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race_anai_ff", "E2061", "PO 2 Race ANAI Free Form is required when code 971 is reported in PO 2 Race."));
                    }
                }
                else
                {
                    // When code "971" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po2RaceAnaiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race_anai_ff", "E2061", "PO 2 Race ANAI Free Form must be left blank when code 971 is not reported in PO 2 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po2RaceAnaiFF) && po2RaceAnaiFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_race_anai_ff", "E1140", "PO 2 Race ANAI Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_asian_ff (assumed at index 49)
                if (fields.Length < 59)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_race_asian_ff", "", "Missing PO 2 Race Asian Free Form - the column must be present."));
                }

                string po2Race1 = fields[56]?.Trim() ?? "";  // PO 1 Race from index 47
                string po2RaceAsianFF = fields[58]?.Trim() ?? "";  // PO 1 Race Asian Free Form from index 49

                // Determine whether the free form field is required based on the presence of code "972" in po_1_race.
                bool requires_AsianFF = false;
                if (!string.IsNullOrWhiteSpace(po2Race1))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po2Race1.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(code => code.Trim())
                                           .ToList();

                    // The free form field is required if one of the codes equals "972".
                    requires_AsianFF = raceCodes.Contains("972");
                }

                if (requires_AsianFF)
                {
                    // When code "972" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po2RaceAsianFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race_asian_ff", "E2081", "PO 2 Race Asian Free Form is required when code 972 is reported in PO 2 Race."));
                    }
                }
                else
                {
                    // When code "972" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po2RaceAsianFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race_asian_ff", "E2081", "PO 2 Race Asian Free Form must be left blank when code 972 is not reported in PO 2 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po2RaceAsianFF) && po2RaceAsianFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_race_asian_ff", "E1160", "PO 2 Race Asian Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_baa_ff (assumed at index 50)
                if (fields.Length < 60)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_race_baa_ff", "", "Missing PO 2 Race BAA Free Form - the column must be present."));
                }

                string po2Race2 = fields[56]?.Trim() ?? "";  // PO 1 Race from index 47
                string po2RaceBaaFF = fields[59]?.Trim() ?? "";  // PO 1 Race BAA Free Form from index 50

                // Determine whether the free form field is required based on the presence of code "973" in po_1_race.
                bool requiresBaa_FF = false;
                if (!string.IsNullOrWhiteSpace(po2Race2))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po2Race2
                                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(code => code.Trim())
                                    .ToList();

                    // The free form field is required if one of the codes equals "973".
                    requiresBaa_FF = raceCodes.Contains("973");
                }

                if (requiresBaa_FF)
                {
                    // When code "973" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po2RaceBaaFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race_baa_ff", "E2101", "PO 2 Race BAA Free Form is required when code 973 is reported in PO 2 Race."));
                    }
                }
                else
                {
                    // When code "973" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po2RaceBaaFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race_baa_ff", "E2101", "PO 2 Race BAA Free Form must be left blank when code 973 is not reported in PO 2 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po2RaceBaaFF) && po2RaceBaaFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_race_baa_ff", "E1180", "PO 2 Race BAA Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_pi_ff (assumed at index 51)
                if (fields.Length < 61)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_race_pi_ff", "", "Missing PO 2 Race PI Free Form - the column must be present."));
                }

                string po2Race3 = fields[56]?.Trim() ?? "";  // PO 1 Race from index 47
                string po2RacePiFF = fields[60]?.Trim() ?? "";  // PO 1 Race PI Free Form from index 51

                // Determine whether the free form field is required based on the presence of code "974" in po_1_race.
                bool requiresPi_FF = false;
                if (!string.IsNullOrWhiteSpace(po2Race3))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po2Race3
                                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(code => code.Trim())
                                        .ToList();

                    // The free form field is required if one of the codes equals "974".
                    requiresPi_FF = raceCodes.Contains("974");
                }

                if (requiresPi_FF)
                {
                    // When code "974" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po2RacePiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race_pi_ff", "E2121", "PO 2 Race PI Free Form is required when code 974 is reported in PO 2 Race."));
                    }
                }
                else
                {
                    // When code "974" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po2RacePiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race_pi_ff", "E2121", "PO 2 Race PI Free Form must be left blank when code 974 is not reported in PO 2 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po2RacePiFF) && po2RacePiFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_race_pi_ff", "E1200", "PO 2 Race PI Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_gender_flag (assumed at index 52)
                if (fields.Length < 62)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_gender_flag", "", "Missing PO 2 Gender Flag - the column must be present."));
                }

                // Retrieve the number of principal owners and the PO 1 Gender Flag
                string numPrincipal_Owners2 = fields[44]?.Trim() ?? "";
                string po2GenderFlag = fields[61]?.Trim() ?? "";

                // Conditional Requirement:
                // - If there is at least one principal owner (numPrincipalOwners is not "0" or not blank), 
                //   then po_1_gender_flag is required and must equal "1", "966", or "988".
                // - If there are no principal owners (numPrincipalOwners equals "0" or is blank), 
                //   then po_1_gender_flag should be left blank.
                if (numPrincipal_Owners2 == "2")
                {
                    // No principal owners; field should be left blank.

                    if (string.IsNullOrWhiteSpace(po2GenderFlag))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_gender_flag", "", "PO 2 Gender Flag is required when there is at least two principal owner."));
                    }
                }
                else
                {
                    // At least one principal owner; field is required.
                    if (!string.IsNullOrWhiteSpace(po2GenderFlag))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_gender_flag", "", "PO 2 Gender Flag must be left blank if there are no principal owners."));
                    }
                    else
                    {
                        // Validate that the flag equals one of the allowed values.
                        if (po2GenderFlag != "1" && po2GenderFlag != "966" && po2GenderFlag != "988")
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_2_gender_flag", "E1220", "PO 2 Gender Flag must equal 1, 966, or 988."));
                        }
                    }
                }


                // Ensure the fields array has enough columns for po_1_gender_ff (assumed at index 53)
                if (fields.Length < 63)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_gender_ff", "", "Missing PO 2 Gender Free Form - the column must be present."));
                }

                string po2GenderFlag1 = fields[61]?.Trim() ?? "";  // PO 1 Gender Flag from index 52
                string po2GenderFF = fields[62]?.Trim() ?? "";    // PO 1 Gender Free Form from index 53

                // Conditional Requirement:
                // - If po_1_gender_flag equals "1", then po_1_gender_ff is required.
                // - If po_1_gender_flag is not "1", then po_1_gender_ff must be left blank.
                if (po2GenderFlag1 == "1")
                {
                    if (string.IsNullOrWhiteSpace(po2GenderFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_gender_ff", "E2141", "PO 2 Gender Free Form is required when PO 2 Gender Flag is 1."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po2GenderFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_gender_ff", "E2141", "PO 2 Gender Free Form must be left blank unless PO 2 Gender Flag is 1."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po2GenderFF) && po2GenderFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_2_gender_ff", "E1240", "PO 2 Gender Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_2_ethnicity (assumed at index 54)
                if (fields.Length < 64)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity", "", "Missing PO 3 Ethnicity - the column must be present."));
                }

                string num_PrincipalOwners3 = fields[44]?.Trim() ?? "";
                string po3Ethnicity = fields[63]?.Trim() ?? "";

                // Conditional Requirement:
                // - If num_principal_owners equals "2", then po_2_ethnicity is required.
                // - If num_principal_owners is not "2" (i.e. fewer than two principal owners), then po_2_ethnicity must be left blank.
                if (num_PrincipalOwners3 == "3")
                {
                    if (string.IsNullOrWhiteSpace(po3Ethnicity))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity", "", "PO 3 Ethnicity is required when there are exactly 3 principal owners."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po3Ethnicity))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity", "", "PO 3 Ethnicity must be left blank if there are fewer than 3 principal owners."));
                    }
                }

                // If a value is provided, perform further validations.
                if (!string.IsNullOrWhiteSpace(po3Ethnicity))
                {
                    // Split the field into individual codes using semicolons as the delimiter.
                    var codes = po3Ethnicity
                                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(code => code.Trim())
                                    .ToList();

                    // Validation: Must contain at least one value.
                    if (codes.Count == 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity", "", "PO 3 Ethnicity must contain at least one value."));
                    }

                    // Validation: Should not contain duplicated values.
                    if (codes.Count != codes.Distinct().Count())
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity", "W1261", "PO 3 Ethnicity must not contain duplicate values."));
                    }

                    // Allowed values for each code.
                    string[] allowedValues = { "1", "11", "12", "13", "14", "2", "966", "977", "988" };

                    // Validate that each code is allowed.
                    foreach (var code in codes)
                    {
                        if (!allowedValues.Contains(code))
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity", "E1260", $"Invalid PO 3 Ethnicity code: {code}. Allowed values are 1, 11, 12, 13, 14, 2, 966, 977, or 988."));
                        }
                    }

                    // Validation: When code 966 or 988 is reported, no other codes should be present.
                    if ((codes.Contains("966") || codes.Contains("988")) && codes.Count > 1)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity", "W1262", "When code 966 or 988 is reported in PO 3 Ethnicity, no other codes should be included."));
                    }
                }

                // Ensure the fields array has enough columns for po_1_ethnicity_ff (assumed at index 46)
                if (fields.Length < 65)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity_ff", "", "Missing PO 3 Ethnicity Free Form - the column must be present."));
                }

                string po3_Ethnicity = fields[63]?.Trim() ?? ""; // PO 2 Ethnicity from index 45
                string po3EthnicityFF = fields[64]?.Trim() ?? ""; // PO 2 Ethnicity Free Form from index 46

                // Determine whether the free form field is required based on the presence of code "977" in po_1_ethnicity.
                bool requiresFreeForm3 = false;
                if (!string.IsNullOrWhiteSpace(po3_Ethnicity))
                {
                    // Split the value into individual codes (using semicolon as the delimiter) and trim each code.
                    var ethnicityCodes = po3_Ethnicity
                                            .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(code => code.Trim())
                                            .ToList();

                    // The free form field is required if one of the codes equals "977".
                    requiresFreeForm3 = ethnicityCodes.Contains("977");
                }

                if (requiresFreeForm3)
                {
                    // When code 977 is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po3EthnicityFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity_ff", "E2042", "PO 3 Ethnicity Free Form is required when code 977 is reported in PO 3 Ethnicity."));
                    }
                }
                else
                {
                    // When code 977 is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po3EthnicityFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity_ff", "E2042", "PO 3 Ethnicity Free Form must be left blank when code 977 is not reported in PO 3 Ethnicity."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po3EthnicityFF) && po3EthnicityFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_ethnicity_ff", "E1280", "PO 3 Ethnicity Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race (assumed at index 47)
                if (fields.Length < 66)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_race", "", "Missing PO 3 Race - the column must be present."));
                }

                string num_Principal_Owners3 = fields[44]?.Trim() ?? ""; // Principal owners count from index 44
                string po3Race = fields[65]?.Trim() ?? ""; // PO 1 Race from index 47

                // Conditional Requirement:
                // - If there is exactly 1 principal owner, then po_1_race is required.
                // - Otherwise, the field should be left blank.
                if (num_Principal_Owners3 == "3")
                {
                    if (string.IsNullOrWhiteSpace(po3Race))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race", "", "PO 3 Race is required when there is exactly 3 principal owner."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po3Race))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race", "", "PO 3 Race must be left blank if there are not exactly 3 principal owner."));
                    }
                }

                // When a value is provided, perform further validations.
                if (!string.IsNullOrWhiteSpace(po3Race))
                {
                    // Split the field into individual race codes using semicolons as the delimiter.
                    var raceCodes = po3Race
                                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(code => code.Trim())
                                        .ToList();

                    // Validation: Must contain at least one value.
                    if (raceCodes.Count == 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race", "", "PO 3 Race must contain at least one value."));
                    }

                    // Validation: Should not contain duplicated values.
                    if (raceCodes.Count != raceCodes.Distinct().Count())
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race", "W1301", "PO 3 Race should not contain duplicate values."));
                    }

                    // Allowed race codes.
                    string[] allowedCodes = {
                                            "1", "2",
                                           "21", "22", "23", "24", "25", "26", "27",
                                                      "3", "31", "32", "33", "34", "35", "36", "37",
                                                        "4", "41", "42", "43", "44",
                                                                   "5",
                                                 "966", "971", "972", "973", "974",
                                                         "988"
                                                      };

                    // Validate that each code is allowed.
                    foreach (var code in raceCodes)
                    {
                        if (!allowedCodes.Contains(code))
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_3_race", "E1300", $"Invalid PO 3 Race code: {code}. Allowed values are 1, 2, 21, 22, 23, 24, 25, 26, 27, 3, 31, 32, 33, 34, 35, 36, 37, 4, 41, 42, 43, 44, 5, 966, 971, 972, 973, 974, or 988."));
                        }
                    }

                    // Special Rule: When code 966 or 988 is reported, no other codes should be present.
                    if ((raceCodes.Contains("966") || raceCodes.Contains("988")) && raceCodes.Count > 1)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race", "W1302", "When code 966 or 988 is reported in PO 3 Race, no other codes should be included."));
                    }
                }

                // Ensure the fields array has enough columns for po_1_race_anai_ff (assumed at index 48)
                if (fields.Length < 67)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_race_anai_ff", "", "Missing PO 3 Race ANAI Free Form - the column must be present."));
                }

                string po3_Race = fields[65]?.Trim() ?? "";  // PO 1 Race from index 47
                string po3Race_AnaiFF = fields[66]?.Trim() ?? ""; // PO 1 Race ANAI Free Form from index 48

                // Determine whether the free form field is required based on the presence of code "971" in po_1_race.
                bool requiresAnai_FFF = false;
                if (!string.IsNullOrWhiteSpace(po3_Race))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po3_Race.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(code => code.Trim())
                                           .ToList();

                    // The free form field is required if one of the codes equals "971".
                    requiresAnai_FFF = raceCodes.Contains("971");
                }

                if (requiresAnai_FFF)
                {
                    // When code "971" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po3Race_AnaiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race_anai_ff", "E2062", "PO 3 Race ANAI Free Form is required when code 971 is reported in PO 3 Race."));
                    }
                }
                else
                {
                    // When code "971" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po3Race_AnaiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race_anai_ff", "E2062", "PO 3 Race ANAI Free Form must be left blank when code 971 is not reported in PO 3 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po3Race_AnaiFF) && po3Race_AnaiFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_race_anai_ff", "E1320", "PO 3 Race ANAI Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_asian_ff (assumed at index 49)
                if (fields.Length < 68)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_race_asian_ff", "", "Missing PO 3 Race Asian Free Form - the column must be present."));
                }

                string po3Race1 = fields[65]?.Trim() ?? "";  // PO 1 Race from index 47
                string po3RaceAsianFF = fields[67]?.Trim() ?? "";  // PO 1 Race Asian Free Form from index 49

                // Determine whether the free form field is required based on the presence of code "972" in po_1_race.
                bool requires_AsianFFF = false;
                if (!string.IsNullOrWhiteSpace(po3Race1))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po3Race1.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(code => code.Trim())
                                           .ToList();

                    // The free form field is required if one of the codes equals "972".
                    requires_AsianFFF = raceCodes.Contains("972");
                }

                if (requires_AsianFFF)
                {
                    // When code "972" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po3RaceAsianFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race_asian_ff", "E2082", "PO 3 Race Asian Free Form is required when code 972 is reported in PO 3 Race."));
                    }
                }
                else
                {
                    // When code "972" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po3RaceAsianFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race_asian_ff", "E2082", "PO 3 Race Asian Free Form must be left blank when code 972 is not reported in PO 3 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po3RaceAsianFF) && po3RaceAsianFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_race_asian_ff", "E1340", "PO 3 Race Asian Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_baa_ff (assumed at index 50)
                if (fields.Length < 69)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_race_baa_ff", "", "Missing PO 3 Race BAA Free Form - the column must be present."));
                }

                string po3Race2 = fields[65]?.Trim() ?? "";  // PO 1 Race from index 47
                string po3RaceBaaFF = fields[68]?.Trim() ?? "";  // PO 1 Race BAA Free Form from index 50

                // Determine whether the free form field is required based on the presence of code "973" in po_1_race.
                bool requiresBaa_FFF = false;
                if (!string.IsNullOrWhiteSpace(po3Race2))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po3Race2
                                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(code => code.Trim())
                                    .ToList();

                    // The free form field is required if one of the codes equals "973".
                    requiresBaa_FFF = raceCodes.Contains("973");
                }

                if (requiresBaa_FFF)
                {
                    // When code "973" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po3RaceBaaFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race_baa_ff", "E2102", "PO 3 Race BAA Free Form is required when code 973 is reported in PO 3 Race."));
                    }
                }
                else
                {
                    // When code "973" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po3RaceBaaFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race_baa_ff", "E2102", "PO 3 Race BAA Free Form must be left blank when code 973 is not reported in PO 3 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po3RaceBaaFF) && po3RaceBaaFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_race_baa_ff", "E1360", "PO 3 Race BAA Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_pi_ff (assumed at index 51)
                if (fields.Length < 70)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_race_pi_ff", "", "Missing PO 3 Race PI Free Form - the column must be present."));
                }

                string po3Race3 = fields[65]?.Trim() ?? "";  // PO 1 Race from index 47
                string po3RacePiFF = fields[69]?.Trim() ?? "";  // PO 1 Race PI Free Form from index 51

                // Determine whether the free form field is required based on the presence of code "974" in po_1_race.
                bool requiresPi_FFF = false;
                if (!string.IsNullOrWhiteSpace(po3Race3))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po3Race3
                                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(code => code.Trim())
                                        .ToList();

                    // The free form field is required if one of the codes equals "974".
                    requiresPi_FFF = raceCodes.Contains("974");
                }

                if (requiresPi_FFF)
                {
                    // When code "974" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po3RacePiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race_pi_ff", "E2122", "PO 3 Race PI Free Form is required when code 974 is reported in PO 3 Race."));
                    }
                }
                else
                {
                    // When code "974" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po3RacePiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_race_pi_ff", "E2122", "PO 3 Race PI Free Form must be left blank when code 974 is not reported in PO 3 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po3RacePiFF) && po3RacePiFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_race_pi_ff", "E1380", "PO 3 Race PI Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_gender_flag (assumed at index 52)
                if (fields.Length < 71)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_gender_flag", "", "Missing PO 3 Gender Flag - the column must be present."));
                }

                // Retrieve the number of principal owners and the PO 1 Gender Flag
                string numPrincipal_Owners3 = fields[44]?.Trim() ?? "";
                string po3GenderFlag = fields[70]?.Trim() ?? "";

                // Conditional Requirement:
                // - If there is at least one principal owner (numPrincipalOwners is not "0" or not blank), 
                //   then po_1_gender_flag is required and must equal "1", "966", or "988".
                // - If there are no principal owners (numPrincipalOwners equals "0" or is blank), 
                //   then po_1_gender_flag should be left blank.
                if (numPrincipal_Owners3 == "3")
                {
                    // No principal owners; field should be left blank.


                    if (string.IsNullOrWhiteSpace(po3GenderFlag))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_gender_flag", "", "PO 3 Gender Flag is required when there is exactly 3 principal owner."));
                    }
                }
                else
                {
                    // At least one principal owner; field is required.

                    if (!string.IsNullOrWhiteSpace(po3GenderFlag))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_gender_flag", "", "PO 3 Gender Flag must be left blank if there are not exactly 3 principal owners."));
                    }
                    else
                    {
                        // Validate that the flag equals one of the allowed values.
                        if (po3GenderFlag != "1" && po3GenderFlag != "966" && po3GenderFlag != "988")
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_3_gender_flag", "E1400", "PO 3 Gender Flag must equal 1, 966, or 988."));
                        }
                    }
                }


                // Ensure the fields array has enough columns for po_1_gender_ff (assumed at index 53)
                if (fields.Length < 72)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_gender_ff", "", "Missing PO 3 Gender Free Form - the column must be present."));
                }

                string po3GenderFlag1 = fields[70]?.Trim() ?? "";  // PO 1 Gender Flag from index 52
                string po3GenderFF = fields[71]?.Trim() ?? "";    // PO 1 Gender Free Form from index 53

                // Conditional Requirement:
                // - If po_1_gender_flag equals "1", then po_1_gender_ff is required.
                // - If po_1_gender_flag is not "1", then po_1_gender_ff must be left blank.
                if (po3GenderFlag1 == "1")
                {
                    if (string.IsNullOrWhiteSpace(po3GenderFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_gender_ff", "E2142", "PO 3 Gender Free Form is required when PO 3 Gender Flag is 1."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po3GenderFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_3_gender_ff", "E2142", "PO 3 Gender Free Form must be left blank unless PO 3 Gender Flag is 1."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po3GenderFF) && po3GenderFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_3_gender_ff", "E1420", "PO 3 Gender Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_2_ethnicity (assumed at index 54)
                if (fields.Length < 73)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity", "", "Missing PO 4 Ethnicity - the column must be present."));
                }

                string numPrincipalOwners4 = fields[44]?.Trim() ?? "";
                string po4Ethnicity = fields[72]?.Trim() ?? "";

                // Conditional Requirement:
                // - If num_principal_owners equals "2", then po_2_ethnicity is required.
                // - If num_principal_owners is not "2" (i.e. fewer than two principal owners), then po_2_ethnicity must be left blank.
                if (numPrincipalOwners4 == "4")
                {
                    if (string.IsNullOrWhiteSpace(po4Ethnicity))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity", "", "PO 4 Ethnicity is required when there are exactly 4 principal owners."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po4Ethnicity))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity", "", "PO 4 Ethnicity must be left blank if there are fewer than 4 principal owners."));
                    }
                }

                // If a value is provided, perform further validations.
                if (!string.IsNullOrWhiteSpace(po4Ethnicity))
                {
                    // Split the field into individual codes using semicolons as the delimiter.
                    var codes = po4Ethnicity
                                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(code => code.Trim())
                                    .ToList();

                    // Validation: Must contain at least one value.
                    if (codes.Count == 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity", "", "PO 4 Ethnicity must contain at least one value."));
                    }

                    // Validation: Should not contain duplicated values.
                    if (codes.Count != codes.Distinct().Count())
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity", "W1441", "PO 4 Ethnicity must not contain duplicate values."));
                    }

                    // Allowed values for each code.
                    string[] allowedValues = { "1", "11", "12", "13", "14", "2", "966", "977", "988" };

                    // Validate that each code is allowed.
                    foreach (var code in codes)
                    {
                        if (!allowedValues.Contains(code))
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity", "E1440", $"Invalid PO 2 Ethnicity code: {code}. Allowed values are 1, 11, 12, 13, 14, 2, 966, 977, or 988."));
                        }
                    }

                    // Validation: When code 966 or 988 is reported, no other codes should be present.
                    if ((codes.Contains("966") || codes.Contains("988")) && codes.Count > 1)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity", "W1442", "When code 966 or 988 is reported in PO 4 Ethnicity, no other codes should be included."));
                    }
                }

                // Ensure the fields array has enough columns for po_1_ethnicity_ff (assumed at index 46)
                if (fields.Length < 74)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity_ff", "", "Missing PO 4 Ethnicity Free Form - the column must be present."));
                }

                string po4_Ethnicity = fields[72]?.Trim() ?? ""; // PO 2 Ethnicity from index 45
                string po4EthnicityFF = fields[73]?.Trim() ?? ""; // PO 2 Ethnicity Free Form from index 46

                // Determine whether the free form field is required based on the presence of code "977" in po_1_ethnicity.
                bool requiresFreeForm4 = false;
                if (!string.IsNullOrWhiteSpace(po4_Ethnicity))
                {
                    // Split the value into individual codes (using semicolon as the delimiter) and trim each code.
                    var ethnicityCodes = po4_Ethnicity
                                            .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(code => code.Trim())
                                            .ToList();

                    // The free form field is required if one of the codes equals "977".
                    requiresFreeForm4 = ethnicityCodes.Contains("977");
                }

                if (requiresFreeForm4)
                {
                    // When code 977 is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po4EthnicityFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity_ff", "E2043", "PO 4 Ethnicity Free Form is required when code 977 is reported in PO 4 Ethnicity."));
                    }
                }
                else
                {
                    // When code 977 is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po4EthnicityFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity_ff", "E2043", "PO 4 Ethnicity Free Form must be left blank when code 977 is not reported in PO 4 Ethnicity."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po4EthnicityFF) && po4EthnicityFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_ethnicity_ff", "E1460", "PO 4 Ethnicity Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race (assumed at index 47)
                if (fields.Length < 75)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_race", "", "Missing PO 4 Race - the column must be present."));
                }

                string num_PrincipalOwners4 = fields[44]?.Trim() ?? ""; // Principal owners count from index 44
                string po4Race = fields[74]?.Trim() ?? ""; // PO 1 Race from index 47

                // Conditional Requirement:
                // - If there is exactly 1 principal owner, then po_1_race is required.
                // - Otherwise, the field should be left blank.
                if (num_PrincipalOwners4 == "4")
                {
                    if (string.IsNullOrWhiteSpace(po4Race))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_race", "", "PO 4 Race is required when there is exactly 4 principal owner."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po4Race))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race", "", "PO 4 Race must be left blank if there are not exactly 4 principal owner."));
                    }
                }

                // When a value is provided, perform further validations.
                if (!string.IsNullOrWhiteSpace(po4Race))
                {
                    // Split the field into individual race codes using semicolons as the delimiter.
                    var raceCodes = po4Race
                                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(code => code.Trim())
                                        .ToList();

                    // Validation: Must contain at least one value.
                    if (raceCodes.Count == 0)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_race", "", "PO 4 Race must contain at least one value."));
                    }

                    // Validation: Should not contain duplicated values.
                    if (raceCodes.Count != raceCodes.Distinct().Count())
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_race", "W1481", "PO 4 Race should not contain duplicate values."));
                    }

                    // Allowed race codes.
                    string[] allowedCodes = {
                                            "1", "2",
                                           "21", "22", "23", "24", "25", "26", "27",
                                                      "3", "31", "32", "33", "34", "35", "36", "37",
                                                        "4", "41", "42", "43", "44",
                                                                   "5",
                                                 "966", "971", "972", "973", "974",
                                                         "988"
                                                      };

                    // Validate that each code is allowed.
                    foreach (var code in raceCodes)
                    {
                        if (!allowedCodes.Contains(code))
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_4_race", "E1480", $"Invalid PO 2 Race code: {code}. Allowed values are 1, 2, 21, 22, 23, 24, 25, 26, 27, 3, 31, 32, 33, 34, 35, 36, 37, 4, 41, 42, 43, 44, 5, 966, 971, 972, 973, 974, or 988."));
                        }
                    }

                    // Special Rule: When code 966 or 988 is reported, no other codes should be present.
                    if ((raceCodes.Contains("966") || raceCodes.Contains("988")) && raceCodes.Count > 1)
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_race", "W1482", "When code 966 or 988 is reported in PO 4 Race, no other codes should be included."));
                    }
                }

                // Ensure the fields array has enough columns for po_1_race_anai_ff (assumed at index 48)
                if (fields.Length < 76)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_race_anai_ff", "", "Missing PO 4 Race ANAI Free Form - the column must be present."));
                }

                string po4_Race = fields[74]?.Trim() ?? "";  // PO 1 Race from index 47
                string po4RaceAnaiFF = fields[75]?.Trim() ?? ""; // PO 1 Race ANAI Free Form from index 48

                // Determine whether the free form field is required based on the presence of code "971" in po_1_race.
                bool requiresAnai_FFFF = false;
                if (!string.IsNullOrWhiteSpace(po4_Race))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po4_Race.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(code => code.Trim())
                                           .ToList();

                    // The free form field is required if one of the codes equals "971".
                    requiresAnai_FFFF = raceCodes.Contains("971");
                }

                if (requiresAnai_FFFF)
                {
                    // When code "971" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po4RaceAnaiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race_anai_ff", "E2063", "PO 4 Race ANAI Free Form is required when code 971 is reported in PO 4 Race."));
                    }
                }
                else
                {
                    // When code "971" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po4RaceAnaiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_2_race_anai_ff", "E2063", "PO 4 Race ANAI Free Form must be left blank when code 971 is not reported in PO 4 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po4RaceAnaiFF) && po4RaceAnaiFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_race_anai_ff", "E1500", "PO 4 Race ANAI Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_asian_ff (assumed at index 49)
                if (fields.Length < 77)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_race_asian_ff", "", "Missing PO 4 Race Asian Free Form - the column must be present."));
                }

                string po4Race1 = fields[74]?.Trim() ?? "";  // PO 1 Race from index 47
                string po4RaceAsianFF = fields[76]?.Trim() ?? "";  // PO 1 Race Asian Free Form from index 49

                // Determine whether the free form field is required based on the presence of code "972" in po_1_race.
                bool requires_AsianFFFF = false;
                if (!string.IsNullOrWhiteSpace(po4Race1))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po4Race1.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(code => code.Trim())
                                           .ToList();

                    // The free form field is required if one of the codes equals "972".
                    requires_AsianFFFF = raceCodes.Contains("972");
                }

                if (requires_AsianFFFF)
                {
                    // When code "972" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po4RaceAsianFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_race_asian_ff", "E2083", "PO 4 Race Asian Free Form is required when code 972 is reported in PO 4 Race."));
                    }
                }
                else
                {
                    // When code "972" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po4RaceAsianFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_race_asian_ff", "E2083", "PO 4 Race Asian Free Form must be left blank when code 972 is not reported in PO 4 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po4RaceAsianFF) && po4RaceAsianFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_race_asian_ff", "E1520", "PO 4 Race Asian Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_baa_ff (assumed at index 50)
                if (fields.Length < 78)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_race_baa_ff", "", "Missing PO 4 Race BAA Free Form - the column must be present."));
                }

                string po4Race2 = fields[74]?.Trim() ?? "";  // PO 1 Race from index 47
                string po4RaceBaaFF = fields[77]?.Trim() ?? "";  // PO 1 Race BAA Free Form from index 50

                // Determine whether the free form field is required based on the presence of code "973" in po_1_race.
                bool requiresBaa_FFFF = false;
                if (!string.IsNullOrWhiteSpace(po4Race2))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po2Race2
                                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(code => code.Trim())
                                    .ToList();

                    // The free form field is required if one of the codes equals "973".
                    requiresBaa_FFFF = raceCodes.Contains("973");
                }

                if (requiresBaa_FFFF)
                {
                    // When code "973" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po4RaceBaaFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_race_baa_ff", "E2103", "PO 4 Race BAA Free Form is required when code 973 is reported in PO 4 Race."));
                    }
                }
                else
                {
                    // When code "973" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po4RaceBaaFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_race_baa_ff", "E2103", "PO 4 Race BAA Free Form must be left blank when code 973 is not reported in PO 4 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po4RaceBaaFF) && po4RaceBaaFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_race_baa_ff", "E1540", "PO 4 Race BAA Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_race_pi_ff (assumed at index 51)
                if (fields.Length < 79)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_race_pi_ff", "", "Missing PO 4 Race PI Free Form - the column must be present."));
                }

                string po4Race3 = fields[74]?.Trim() ?? "";  // PO 1 Race from index 47
                string po4RacePiFF = fields[78]?.Trim() ?? "";  // PO 1 Race PI Free Form from index 51

                // Determine whether the free form field is required based on the presence of code "974" in po_1_race.
                bool requiresPi_FFFF = false;
                if (!string.IsNullOrWhiteSpace(po4Race3))
                {
                    // Split the race codes by semicolon and trim each code.
                    var raceCodes = po4Race3
                                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(code => code.Trim())
                                        .ToList();

                    // The free form field is required if one of the codes equals "974".
                    requiresPi_FFFF = raceCodes.Contains("974");
                }

                if (requiresPi_FFFF)
                {
                    // When code "974" is reported, the free form field is required.
                    if (string.IsNullOrWhiteSpace(po4RacePiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_race_pi_ff", "E2123", "PO 4 Race PI Free Form is required when code 974 is reported in PO 4 Race."));
                    }
                }
                else
                {
                    // When code "974" is not present, the free form field should be left blank.
                    if (!string.IsNullOrWhiteSpace(po4RacePiFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_race_pi_ff", "E2123", "PO 4 Race PI Free Form must be left blank when code 974 is not reported in PO 4 Race."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po4RacePiFF) && po4RacePiFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_race_pi_ff", "E1560", "PO 4 Race PI Free Form must not exceed 300 characters."));
                }

                // Ensure the fields array has enough columns for po_1_gender_flag (assumed at index 52)
                if (fields.Length < 80)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_gender_flag", "", "Missing PO 4 Gender Flag - the column must be present."));
                }

                // Retrieve the number of principal owners and the PO 1 Gender Flag
                string numPrincipal_Owners4 = fields[44]?.Trim() ?? "";
                string po4GenderFlag = fields[79]?.Trim() ?? "";

                // Conditional Requirement:
                // - If there is at least one principal owner (numPrincipalOwners is not "0" or not blank), 
                //   then po_1_gender_flag is required and must equal "1", "966", or "988".
                // - If there are no principal owners (numPrincipalOwners equals "0" or is blank), 
                //   then po_1_gender_flag should be left blank.
                if (numPrincipal_Owners4 == "4")
                {
                    // No principal owners; field should be left blank.

                    if (string.IsNullOrWhiteSpace(po4GenderFlag))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_gender_flag", "", "PO 4 Gender Flag is required when there is at least four principal owner."));
                    }
                }
                else
                {
                    // At least one principal owner; field is required.
                    if (!string.IsNullOrWhiteSpace(po4GenderFlag))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_gender_flag", "", "PO 4 Gender Flag must be left blank if there are no principal owners."));
                    }
                    else
                    {
                        // Validate that the flag equals one of the allowed values.
                        if (po4GenderFlag != "1" && po4GenderFlag != "966" && po4GenderFlag != "988")
                        {
                            validationErrors.Add(new ValidationError(rowNumber, "po_4_gender_flag", "E1580", "PO 4 Gender Flag must equal 1, 966, or 988."));
                        }
                    }
                }


                // Ensure the fields array has enough columns for po_1_gender_ff (assumed at index 53)
                if (fields.Length < 81)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_gender_ff", "", "Missing PO 4 Gender Free Form - the column must be present."));
                }

                string po4GenderFlag1 = fields[79]?.Trim() ?? "";  // PO 1 Gender Flag from index 52
                string po4GenderFF = fields[80]?.Trim() ?? "";    // PO 1 Gender Free Form from index 53

                // Conditional Requirement:
                // - If po_1_gender_flag equals "1", then po_1_gender_ff is required.
                // - If po_1_gender_flag is not "1", then po_1_gender_ff must be left blank.
                if (po4GenderFlag1 == "1")
                {
                    if (string.IsNullOrWhiteSpace(po4GenderFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_gender_ff", "E2143", "PO 4 Gender Free Form is required when PO 4 Gender Flag is 1."));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(po4GenderFF))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "po_4_gender_ff", "E2143", "PO 4 Gender Free Form must be left blank unless PO 4 Gender Flag is 1."));
                    }
                }

                // Validate that, when present, the free form text does not exceed 300 characters.
                if (!string.IsNullOrWhiteSpace(po4GenderFF) && po4GenderFF.Length > 300)
                {
                    validationErrors.Add(new ValidationError(rowNumber, "po_4_gender_ff", "E1600", "PO 4 Gender Free Form must not exceed 300 characters."));
                }


                // Retrieve the value of action_taken (replace actionTakenIndex with the actual index)
                string actionTaken5 = fields[15]?.Trim() ?? "";

                // Check if action_taken is "3", "4", or "5"
                if (actionTaken5 == "3" || actionTaken5 == "4" || actionTaken5 == "5")
                {
                    // Retrieve the pricing-related fields (replace the index variables with actual indexes)
                    string pricingInterestRate_Type = fields[19]?.Trim() ?? "";
                    string pricingMca_AddcostFlag = fields[29]?.Trim() ?? "";
                    string pricing_PrepenaltyAllowed = fields[31]?.Trim() ?? "";
                    string pricing_PrepenaltyExists = fields[32]?.Trim() ?? "";
                    string pricing_OriginationCharges = fields[26]?.Trim() ?? "";
                    string pricing_BrokerFees = fields[27]?.Trim() ?? "";
                    string pricing_InitialCharges = fields[28]?.Trim() ?? "";

                    // Check the condition:
                    // - All of the following must be true for the record to be valid:
                    //      pricing_interest_rate_type == "999"
                    //      pricing_mca_addcost_flag == "999"
                    //      pricing_prepenalty_allowed == "999"
                    //      pricing_prepenalty_exists == "999"
                    //      pricing_origination_charges is blank
                    //      pricing_broker_fees is blank
                    //      pricing_initial_charges is blank
                    if (pricingInterestRate_Type != "999" ||
                        pricingMca_AddcostFlag != "999" ||
                        pricing_PrepenaltyAllowed != "999" ||
                        pricing_PrepenaltyExists != "999" ||
                        !string.IsNullOrWhiteSpace(pricing_OriginationCharges) ||
                        !string.IsNullOrWhiteSpace(pricing_BrokerFees) ||
                        !string.IsNullOrWhiteSpace(pricing_InitialCharges))
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_fields", "E2014", "pricing_all.conditional_fieldset_conflict"));



                    }
                }

                // Retrieve values from the CSV fields (adjust the indexes accordingly)
                string actionTaken6 = fields[15]?.Trim() ?? "";
                string pricingOrigination_Charges = fields[26]?.Trim() ?? "";
                string pricingBroker_Fees = fields[27]?.Trim() ?? "";
                string pricingInitial_Charges = fields[28]?.Trim() ?? "";
                string pricingPrepenalty_Allowed = fields[31]?.Trim() ?? "";
                string pricingPrepenalty_Exists = fields[32]?.Trim() ?? "";

                // Apply the validation logic when action_taken is "1" or "2"
                if (actionTaken6 == "1" || actionTaken6 == "2")
                {
                    // Check if any of the following conditions are true:
                    // - pricing_origination_charges is blank
                    // - pricing_broker_fees is blank
                    // - pricing_initial_charges is blank
                    // - pricing_prepenalty_allowed equals "999"
                    // - pricing_prepenalty_exists equals "999"
                    if (string.IsNullOrWhiteSpace(pricingOrigination_Charges) ||
                        string.IsNullOrWhiteSpace(pricingBroker_Fees) ||
                        string.IsNullOrWhiteSpace(pricingInitial_Charges) ||
                        pricingPrepenalty_Allowed == "999" ||
                        pricingPrepenalty_Exists == "999")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_fields", "E2015", "pricing_charges.conditional_fieldset_conflict\r\n"));

                    }
                }

                // Assume these values are already retrieved and trimmed from the data source
                string pricingInterestRateType1 = fields[19]?.Trim() ?? "";
                string pricingAdjIndexName1 = fields[23]?.Trim() ?? "";

                // Check if pricing_interest_rate_type is not one of "1", "3", or "5"
                if (pricingInterestRateType1 != "1" && pricingInterestRateType1 != "3" && pricingInterestRateType1 != "5")
                {
                    // Then pricing_adj_index_name must equal "999"
                    if (pricingAdjIndexName1 != "999")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_name",
                            "E2019", "Invalid combination: When pricing_interest_rate_type is not 1, 3, or 5, pricing_adj_index_name must equal 999."

                        ));
                    }
                }
                else // pricing_interest_rate_type is equal to "1", "3", or "5"
                {
                    // Then pricing_adj_index_name must not equal "999"
                    if (pricingAdjIndexName1 == "999")
                    {
                        validationErrors.Add(new ValidationError(rowNumber, "pricing_adj_index_name",
                           "E2019", "Invalid combination: When pricing_interest_rate_type is 1, 3, or 5, pricing_adj_index_name must not equal 999."
                        ));
                    }
                }

                // Retrieve the values from your data source (adjust the array indexes as needed)
                string ctCreditProduct1 = fields[4]?.Trim() ?? "";
                string pricingMcaAddcostFlag1 = fields[29]?.Trim() ?? "";

                // If ct_credit_product is not equal to "7", "8", or "977", then pricing_mca_addcost_flag must equal "999"
                if (ctCreditProduct1 != "7" && ctCreditProduct1 != "8" && ctCreditProduct1 != "977")
                {
                    if (pricingMcaAddcostFlag1 != "999")
                    {
                        validationErrors.Add(new ValidationError(
                            rowNumber,
                            "pricing_mca_addcost_flag", "E2022",
                            "Invalid combination: When ct_credit_product is not 7, 8, or 977, pricing_mca_addcost_flag must equal 999."
                        ));
                    }
                }

                // Retrieve date strings from the CSV fields (replace appDateIndex and actionTakenDateIndex with actual indexes)
                string appDate_Str = fields[1]?.Trim() ?? "";
                string actionTaken_DateStr = fields[16]?.Trim() ?? "";

                // Try to parse the dates
                if (DateTime.TryParse(appDate_Str, out DateTime appDate2) && DateTime.TryParse(actionTaken_DateStr, out DateTime action_TakenDate))
                {
                    // Calculate the difference in days between the action taken date and the application date.
                    double daysDifference = (action_TakenDate - appDate2).TotalDays;

                    // If the application date is more than 730 days before the action taken date, add an error.
                    if (daysDifference > 730)
                    {
                        validationErrors.Add(new ValidationError(
                            rowNumber,
                            "app_date", "W2010",
                            "The application date should be within 730 days (less than two years) before the action taken date."
                        ));
                    }
                }
                else
                {
                    // Optionally handle date parsing errors.
                    validationErrors.Add(new ValidationError(
                         rowNumber,
                         "date_fields", "",
                         "Invalid date format for application date or action taken date."
                    ));
                }

                // Retrieve and trim the values for the two fields (adjust the indexes as needed)
                string typeOfGuarantee = fields[6]?.Trim() ?? "";
                string otherGuarantee = fields[7]?.Trim() ?? "";

                // Split the 'Type of guarantee' field by semicolon into individual codes
                var guaranteeCodes = typeOfGuarantee
                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(code => code.Trim())
                    .Where(code => !string.IsNullOrEmpty(code))
                    .ToList();

                // Exclude code "977" from the count (it does not count toward the five-value maximum)
                int countGuaranteeCodes = guaranteeCodes.Count(code => code != "977");

                // Count the free-form text field as one value if it is not blank
                int freeFormCount = string.IsNullOrWhiteSpace(otherGuarantee) ? 0 : 1;

                // Calculate the combined total number of values
                int totalCount = countGuaranteeCodes + freeFormCount;

                // Validate that the total count does not exceed five
                if (totalCount > 5)
                {
                    validationErrors.Add(new ValidationError(
                        rowNumber,
                        "guarantee_fields", "W2002",
                        "The combined number of values in 'Type of guarantee' and 'Free-form text field for other guarantee' must not exceed five." +
                        " (Note: Code 977 is excluded from this count.)"
                    ));
                }

                // Retrieve and trim the values for the two fields (adjust the indexes as needed)
                string creditPurpose1 = fields[4]?.Trim() ?? "";
                string otherCreditPurpose = fields[5]?.Trim() ?? "";

                // Split the 'Credit purpose' field by semicolon into individual codes
                var creditPurposeCodes = creditPurpose1
                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(code => code.Trim())
                    .Where(code => !string.IsNullOrEmpty(code))
                    .ToList();

                // Exclude code "977" from the count (it does not count toward the maximum allowed total)
                int countCreditPurposeCodes = creditPurposeCodes.Count(code => code != "977");

                // Count the free-form text field as one value if it is not blank
                int freeFormCount1 = string.IsNullOrWhiteSpace(otherCreditPurpose) ? 0 : 1;

                // Calculate the combined total number of values
                int totalCombined = countCreditPurposeCodes + freeFormCount1;

                // Validate that the total combined number does not exceed three
                if (totalCombined > 3)
                {
                    validationErrors.Add(new ValidationError(
                        rowNumber,
                        "credit_purpose_fields", "W2006",
                        "The combined number of values in 'Credit purpose' and 'free-form text field for other credit purpose' must not exceed three. " +
                        "(Note: Code 977 is excluded from this count.)"
                    ));
                }

                // Retrieve and trim the values for the two fields (adjust the indexes as needed)
                string denialReasons2 = fields[17]?.Trim() ?? "";
                string otherDenialReasons = fields[18]?.Trim() ?? "";

                // Split the 'Denial reason(s)' field by semicolon into individual codes
                var denialReasonCodes = denialReasons2
                    .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(code => code.Trim())
                    .Where(code => !string.IsNullOrEmpty(code))
                    .ToList();

                // Exclude code "977" from the count (it does not count toward the maximum allowed total)
                int countDenialReasonCodes = denialReasonCodes.Count(code => code != "977");

                // Count the free-form text field as one value if it is not blank
                int freeFormCount2 = string.IsNullOrWhiteSpace(otherDenialReasons) ? 0 : 1;

                // Calculate the combined total number of values
                int totalCombined1 = countDenialReasonCodes + freeFormCount2;

                // Validate that the combined total does not exceed four
                if (totalCombined1 > 4)
                {
                    validationErrors.Add(new ValidationError(
                        rowNumber,
                        "denial_reason_fields", "W2013",
                        "The combined number of values in 'Denial reason(s)' and 'free-form text field for other denial reason(s)' must not exceed four. " +
                        "(Note: Code 977 is excluded from this count.)"
                    ));
                }

                // Example index definitions (adjust these to match your CSV structure)
                int numPrincipalOwnersIndex = 44;  // e.g., column index for num_principal_owners
                int po1EthnicityIndex = 45;
                int po1RaceIndex = 47;
                int po1GenderFlagIndex = 52;
                int po2EthnicityIndex = 54;
                int po2RaceIndex = 56;
                int po2GenderFlagIndex = 61;
                int po3EthnicityIndex = 63;
                int po3RaceIndex = 65;
                int po3GenderFlagIndex = 70;
                int po4EthnicityIndex = 72;
                int po4RaceIndex = 74;
                int po4GenderFlagIndex = 79;

                // Retrieve and trim field values
                string numPrincipalOwners5 = fields[numPrincipalOwnersIndex]?.Trim() ?? "";
                string po1Ethnicity5 = fields[po1EthnicityIndex]?.Trim() ?? "";
                string po1Race5 = fields[po1RaceIndex]?.Trim() ?? "";
                string po1GenderFlag5 = fields[po1GenderFlagIndex]?.Trim() ?? "";
                string po2Ethnicity5 = fields[po2EthnicityIndex]?.Trim() ?? "";
                string po2Race5 = fields[po2RaceIndex]?.Trim() ?? "";
                string po2GenderFlag5 = fields[po2GenderFlagIndex]?.Trim() ?? "";
                string po3Ethnicity5 = fields[po3EthnicityIndex]?.Trim() ?? "";
                string po3Race5 = fields[po3RaceIndex]?.Trim() ?? "";
                string po3GenderFlag5 = fields[po3GenderFlagIndex]?.Trim() ?? "";
                string po4Ethnicity5 = fields[po4EthnicityIndex]?.Trim() ?? "";
                string po4Race5 = fields[po4RaceIndex]?.Trim() ?? "";
                string po4GenderFlag5 = fields[po4GenderFlagIndex]?.Trim() ?? "";

                // Assume validationWarnings is a List<ValidationWarning>
                // and rowNumber is the current row number
                // Example: List<ValidationWarning> validationWarnings = new List<ValidationWarning>();

                // Validation logic for principal owner fields based on the number of principal owners
                if (string.IsNullOrWhiteSpace(numPrincipalOwners5) || numPrincipalOwners5 == "0")
                {
                    // When no principal owners are indicated, all principal owner fields should be blank.
                    if (!string.IsNullOrWhiteSpace(po1Ethnicity5) || !string.IsNullOrWhiteSpace(po1Race5) || !string.IsNullOrWhiteSpace(po1GenderFlag5) ||
                        !string.IsNullOrWhiteSpace(po2Ethnicity5) || !string.IsNullOrWhiteSpace(po2Race5) || !string.IsNullOrWhiteSpace(po2GenderFlag5) ||
                        !string.IsNullOrWhiteSpace(po3Ethnicity5) || !string.IsNullOrWhiteSpace(po3Race5) || !string.IsNullOrWhiteSpace(po3GenderFlag5) ||
                        !string.IsNullOrWhiteSpace(po4Ethnicity5) || !string.IsNullOrWhiteSpace(po4Race5) || !string.IsNullOrWhiteSpace(po4GenderFlag5))
                    {
                        validationErrors.Add(new ValidationError(
                            rowNumber,
                            "principal_owner_fields", "W2035",
                            "Warning: When num_principal_owners is 0 or blank, all principal owner fields should be blank."
                        ));
                    }
                }
                else if (numPrincipalOwners5 == "1")
                {
                    // For exactly 1 principal owner:
                    //   • Owner 1 fields must be provided.
                    //   • Owners 2–4 fields must be blank.
                    bool owner1Provided = !(string.IsNullOrWhiteSpace(po1Ethnicity5) &&
                                            string.IsNullOrWhiteSpace(po1Race5) &&
                                            string.IsNullOrWhiteSpace(po1GenderFlag5));
                    if (!owner1Provided)
                    {
                        validationErrors.Add(new ValidationError(
                            rowNumber,
                            "po1_fields", "W2036",
                            "Warning: For 1 principal owner, principal owner 1 fields must be provided."
                        ));
                    }
                    if (!string.IsNullOrWhiteSpace(po2Ethnicity5) || !string.IsNullOrWhiteSpace(po2Race5) || !string.IsNullOrWhiteSpace(po2GenderFlag5) ||
                        !string.IsNullOrWhiteSpace(po3Ethnicity5) || !string.IsNullOrWhiteSpace(po3Race5) || !string.IsNullOrWhiteSpace(po3GenderFlag5) ||
                        !string.IsNullOrWhiteSpace(po4Ethnicity5) || !string.IsNullOrWhiteSpace(po4Race5) || !string.IsNullOrWhiteSpace(po4GenderFlag5))
                    {
                        validationErrors.Add(new ValidationError(
                            rowNumber,
                            "other_po_fields", "W2036",
                            "Warning: For 1 principal owner, fields for principal owners 2, 3, and 4 should be blank."
                        ));
                    }
                }
                else if (numPrincipalOwners5 == "2")
                {
                    // For exactly 2 principal owners:
                    //   • Owner 1 and Owner 2 fields must be provided.
                    //   • Owners 3 and 4 fields must be blank.
                    bool owner1Provided = !(string.IsNullOrWhiteSpace(po1Ethnicity5) &&
                                            string.IsNullOrWhiteSpace(po1Race5) &&
                                            string.IsNullOrWhiteSpace(po1GenderFlag5));
                    bool owner2Provided = !(string.IsNullOrWhiteSpace(po2Ethnicity5) &&
                                            string.IsNullOrWhiteSpace(po2Race5) &&
                                            string.IsNullOrWhiteSpace(po2GenderFlag5));
                    if (!owner1Provided || !owner2Provided)
                    {
                        validationErrors.Add(new ValidationError(
                            rowNumber,
                            "po1_po2_fields", "W2037",
                            "Warning: For 2 principal owners, fields for principal owners 1 and 2 must be provided."
                        ));
                    }
                    if (!string.IsNullOrWhiteSpace(po3Ethnicity5) || !string.IsNullOrWhiteSpace(po3Race5) || !string.IsNullOrWhiteSpace(po3GenderFlag5) ||
                        !string.IsNullOrWhiteSpace(po4Ethnicity5) || !string.IsNullOrWhiteSpace(po4Race5) || !string.IsNullOrWhiteSpace(po4GenderFlag5))
                    {
                        validationErrors.Add(new ValidationError(
                            rowNumber,
                            "po3_po4_fields", "W2037",
                            "Warning: For 2 principal owners, fields for principal owners 3 and 4 should be blank."
                        ));
                    }
                }
                else if (numPrincipalOwners5 == "3")
                {
                    // For exactly 3 principal owners:
                    //   • Owner 1, 2, and 3 fields must be provided.
                    //   • Owner 4 fields must be blank.
                    bool owner1Provided = !(string.IsNullOrWhiteSpace(po1Ethnicity5) &&
                                            string.IsNullOrWhiteSpace(po1Race5) &&
                                            string.IsNullOrWhiteSpace(po1GenderFlag5));
                    bool owner2Provided = !(string.IsNullOrWhiteSpace(po2Ethnicity5) &&
                                            string.IsNullOrWhiteSpace(po2Race5) &&
                                            string.IsNullOrWhiteSpace(po2GenderFlag5));
                    bool owner3Provided = !(string.IsNullOrWhiteSpace(po3Ethnicity5) &&
                                            string.IsNullOrWhiteSpace(po3Race5) &&
                                            string.IsNullOrWhiteSpace(po3GenderFlag5));
                    if (!owner1Provided || !owner2Provided || !owner3Provided)
                    {
                        validationErrors.Add(new ValidationError(
                            rowNumber,
                            "po1_po2_po3_fields", "W2038",
                            "Warning: For 3 principal owners, fields for principal owners 1, 2, and 3 must be provided."
                        ));
                    }
                    if (!string.IsNullOrWhiteSpace(po4Ethnicity5) || !string.IsNullOrWhiteSpace(po4Race5) || !string.IsNullOrWhiteSpace(po4GenderFlag5))
                    {
                        validationErrors.Add(new ValidationError(
                            rowNumber,
                            "po4_fields", "W2038",
                            "Warning: For 3 principal owners, fields for principal owner 4 should be blank."
                        ));
                    }
                }
                else if (numPrincipalOwners5 == "4")
                {
                    // For exactly 4 principal owners, fields for all four principal owners must be provided.
                    bool owner1Provided = !(string.IsNullOrWhiteSpace(po1Ethnicity5) &&
                                            string.IsNullOrWhiteSpace(po1Race5) &&
                                            string.IsNullOrWhiteSpace(po1GenderFlag5));
                    bool owner2Provided = !(string.IsNullOrWhiteSpace(po2Ethnicity5) &&
                                            string.IsNullOrWhiteSpace(po2Race5) &&
                                            string.IsNullOrWhiteSpace(po2GenderFlag5));
                    bool owner3Provided = !(string.IsNullOrWhiteSpace(po3Ethnicity5) &&
                                            string.IsNullOrWhiteSpace(po3Race5) &&
                                            string.IsNullOrWhiteSpace(po3GenderFlag5));
                    bool owner4Provided = !(string.IsNullOrWhiteSpace(po4Ethnicity5) &&
                                            string.IsNullOrWhiteSpace(po4Race5) &&
                                            string.IsNullOrWhiteSpace(po4GenderFlag5));
                    if (!owner1Provided || !owner2Provided || !owner3Provided || !owner4Provided)
                    {
                        validationErrors.Add(new ValidationError(
                            rowNumber,
                            "all_po_fields", "W2039",
                            "Warning: For 4 principal owners, fields for all four principal owners must be provided."
                        ));
                    }
                }



            }
        }

        private bool IsValidDateYYYYMMDD(string dateStr)
        {
            return DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ExportErrorsToCsv();
        }

        private void ExportErrorsToCsv()
        {
            if (!validationErrors.Any())
            {
                MessageBox.Show("No errors to export.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx";
                saveFileDialog.Title = "Save Validation Errors";
                saveFileDialog.FileName = "ValidationErrors.csv";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;

                    if (filePath.EndsWith(".csv"))
                    {
                        SaveAsCsv(filePath);
                    }
                    else if (filePath.EndsWith(".xlsx"))
                    {
                        SaveAsExcel(filePath);
                    }

                    MessageBox.Show("Errors exported successfully!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void SaveAsCsv(string filePath)
        {
            StringBuilder csvContent = new StringBuilder();
            csvContent.AppendLine("Row Number,Column Name,Validation ID,Error Message");

            foreach (var error in validationErrors)
            {
                csvContent.AppendLine($"{error.RowNumber},{error.ColumnName},{error.ValidationID},\"{error.ErrorMessage}\"");
            }

            File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);
        }

        private void SaveAsExcel(string filePath)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Validation Errors");

                worksheet.Cells[1, 1].Value = "Row Number";
                worksheet.Cells[1, 2].Value = "Column Name";
                worksheet.Cells[1, 3].Value = "Validation ID";
                worksheet.Cells[1, 4].Value = "Error Message";

                worksheet.Cells.AutoFitColumns();
                package.SaveAs(new FileInfo(filePath));
            }
        }

        
    }

    public class ValidationError : IComparable<ValidationError>
    {
        public int RowNumber { get; set; }
        public string ColumnName { get; set; }

        public string ValidationID { get; set; }
        public string ErrorMessage { get; set; }

        public ValidationError(int rowNumber, string columnName, string validationID, string errorMessage)
        {
            RowNumber = rowNumber;
            ColumnName = columnName;
            ValidationID = validationID;
            ErrorMessage = errorMessage;
        }
        // public int CompareTo(ValidationError other)
        //{
        //    if (other == null) return 1;

        //   int rowComparison = this.RowNumber.CompareTo(other.RowNumber);
        //  if (rowComparison != 0)
        //      return rowComparison;

        //   return string.Compare(this.ColumnName, other.ColumnName, StringComparison.OrdinalIgnoreCase);
        // }

        public static List<string> FieldOrder = new List<string>
{               "uid","app_date","app_method","app_recipient","ct_credit_product",
            "ct_credit_product_ff","ct_guarantee","ct_guarantee_ff","ct_loan_term_flag",
            "ct_loan_term","credit_purpose","credit_purpose_ff","amount_applied_for_flag",
            "amount_applied_for","amount_approved","action_taken","action_taken_date","denial_reasons",
            "denial_reasons_ff","pricing_interest_rate_type","pricing_init_rate_period","pricing_fixed_rate",
            "pricing_adj_margin","pricing_adj_index_name","pricing_adj_index_name_ff","pricing_adj_index_value",
            "pricing_origination_charges","pricing_broker_fees","pricing_initial_charges","pricing_mca_addcost_flag",
            "pricing_mca_addcost","pricing_prepenalty_allowed","pricing_prepenalty_exists","census_tract_adr_type",
            "census_tract_number","gross_annual_revenue_flag","gross_annual_revenue","naics_code_flag","naics_code",
            "number_of_workers","time_in_business_type","time_in_business","business_ownership_status",
            "num_principal_owners_flag","num_principal_owners","po_1_ethnicity","po_1_ethnicity_ff",
            "po_1_race","po_1_race_anai_ff","po_1_race_asian_ff","po_1_race_baa_ff","po_1_race_pi_ff",
            "po_1_gender_flag","po_1_gender_ff","po_2_ethnicity","po_2_ethnicity_ff","po_2_race",
            "po_2_race_anai_ff","po_2_race_asian_ff","po_2_race_baa_ff","po_2_race_pi_ff",
            "po_2_gender_flag","po_2_gender_ff","po_3_ethnicity","po_3_ethnicity_ff",
            "po_3_race","po_3_race_anai_ff","po_3_race_asian_ff","po_3_race_baa_ff",
            "po_3_race_pi_ff","po_3_gender_flag","po_3_gender_ff","po_4_ethnicity",
            "po_4_ethnicity_ff","po_4_race","po_4_race_anai_ff","po_4_race_asian_ff",
            "po_4_race_baa_ff","po_4_race_pi_ff","po_4_gender_flag","po_4_gender_ff"
              
    // … add all field names in your desired order
};

        public int CompareTo(ValidationError other)
        {
            if (other == null) return 1;

            int rowComparison = this.RowNumber.CompareTo(other.RowNumber);
            if (rowComparison != 0)
                return rowComparison;

            // Look up the index of each field in the FieldOrder list.
            int thisIndex = FieldOrder.IndexOf(this.ColumnName);
            int otherIndex = FieldOrder.IndexOf(other.ColumnName);

            // If a column isn't found, treat it as having the lowest priority (i.e. a large index).
            if (thisIndex < 0) thisIndex = int.MaxValue;
            if (otherIndex < 0) otherIndex = int.MaxValue;

            return thisIndex.CompareTo(otherIndex);
        }

    }
}
