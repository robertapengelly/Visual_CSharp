using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CSV_File_Merger
{
    public partial class merger : Form
    {
        private List<string> files = new List<string>();

        public merger()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (browser.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            // Clears all previous items in listbox ready for the new file merge.
            activity.Items.Clear();
            // Clears all previous items in combo box to only show items related to the new search.
            cboLayouts.Items.Clear();
            // Clears all files that was used from the last merge.
            files.Clear();

            // Disables the save button and the combo box incase they are not needed.
            btnSave.Enabled = cboLayouts.Enabled = false;

            // Loop through all files located in the selected directory.
            foreach (System.IO.FileInfo fi in new System.IO.DirectoryInfo(browser.SelectedPath).GetFiles())
            {
                // If the file extension equals .csv then add the full file name to the files list ready for later.
                if (fi.Extension == ".csv")
                    files.Add(fi.FullName);
            }

            if (files.Count == 0)
            {
                // If no CSV files were found then we can't do a merge.
                activity.Items.Add("No CSV files found in " + browser.SelectedPath);
                return;
            }
            else if (files.Count == 1)
            {
                // If only one file is found then there's no point in merging as you'll get the same results.
                activity.Items.Add("Only 1 CSV file was found in " + browser.SelectedPath);
                return;
            }

            // Tells the user how many CSV files were found.
            activity.Items.Add(files.Count.ToString() + " CSV files were found in " + browser.SelectedPath);

            // Get all the different layout's that the CSV files have.
            getLayouts();

            if (cboLayouts.Items.Count == 0)
            {
                // If there's no layout's within any of the files then they may not be CSV files.
                activity.Items.Add("No CSV layouts were found.");
                return;
            }

            // Enable the save button.
            btnSave.Enabled = true;
            
            // Select the first layout by default.
            cboLayouts.SelectedIndex = 0;

            // If there's only one layout found in all CSV files then it's pointless giving the options to select one.
            if (cboLayouts.Items.Count == 1)
                activity.Items.Add("Only 1 layout was found.");
            else if(cboLayouts.Items.Count > 1)
            {
                activity.Items.Add(cboLayouts.Items.Count + " different layouts were found.");
                cboLayouts.Enabled = true;
            }
        }

        private void getLayouts()
        {
            // Loops through all the CSV files found.
            foreach (string file in files)
            {
                string line = null;

                try
                {
                    // Try reading the first line of the file as that's how we will determine if the layout is different.
                    // If the file cannot be accessed for whatever reason then we display the error in the listbox for the user
                    // to see the problem.  Replacing any spaces before or after commas is optional.
                    System.IO.StreamReader reader = new System.IO.StreamReader(file);
                    line = reader.ReadLine().ToLower().Replace(", ", ",").Replace(" ,", ",").Replace (" , ", ",");
                    reader.Close();
                }
                catch (Exception ex)
                {
                    activity.Items.Add(ex.Message);
                }

                // If the file was read successfully and the combo box doesn't already have the layout then add it.
                if ((line != null) && !cboLayouts.Items.Contains(line))
                    cboLayouts.Items.Add(line);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (save.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            mergeColumns(save.FileName);
        }

        private void mergeColumns(string filename)
        {
            // Variable that holds the content's of the new file that will be created.
            string contents = cboLayouts.SelectedItem.ToString();
            // Seperates the combo box options so that we can get the corrent data from other files.
            string[] layout = contents.Split(',');

            foreach (string file in files)
            {
                string line = null;
                
                try
                {
                    // Try reading the first line of the file as that's how we will determine if the layout is different.
                    // If the file cannot be accessed for whatever reason then we display the error in the listbox for the user
                    // to see the problem.  Replacing any spaces before or after commas is optional.
                    System.IO.StreamReader reader = new System.IO.StreamReader(file);
                    line = reader.ReadLine().ToLower().Replace(", ", ",").Replace(" ,", ",").Replace(" , ", ",");

                    // Checks if the line of text was empty the file may not have any data in it.
                    if (line == null)
                        continue;

                    // Splits that line into seperate items to retrieve the relavent data.
                    string[] args = line.Split(',');

                    // If the array length is different to the lines array length then we have got different layouts so
                    // they do not match.
                    if (args.Length != layout.Length) {
                        reader.Close();
                        activity.Items.Add(file + " does not match the selected layout.");
                        continue;
                    }

                    // If the two lengths are the same then loop through each line array item.
                    for (var i = 0; i < args.Length; i++)
                    {
                        // Create a list from the layout array.
                        List<string> list = new List<string>(layout);

                        // If the list doesn't contain the line array items in question then the files are diffent
                        if (!list.Contains(args[i]))
                        {
                            reader.Close();
                            // Making the reader = null is so we can find out if the files were different.
                            reader = null;
                            activity.Items.Add(file + " does not match the selected layout.");
                            break;
                        }
                    }

                    // We set the reader to null if the files were different so we can just skip this file as we can't read
                    // from nothing.
                    if (reader == null)
                        continue;


                    // Read each line until there isn't any lines left.
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Split the line up so we can retrieve the relavent data.
                        string[] data = line.Split(',');

                        // Add a new line to the new file contents ready.
                        contents += Environment.NewLine;

                        // Loop through all the items from the first line of the file.
                        for (var l = 0; l < layout.Length; l++) 
                        {
                            // Loop through all the items from the selected layout.
                            for (var i = 0; i < args.Length; i++)
                            {
                                // If the files item matches the layout item then we have the corrent data so append it to the
                                // new file contents.
                                if (args[i] == layout[l])
                                {
                                    contents += data[i];
                                    //MessageBox.Show(args[i] + ", " + layout[l]);
                                }
                            }

                            // If we are not on the last item then add a comma as it is needed.
                            if (l < layout.Length - 1)
                                contents += ",";
                        }
                    }

                    // Finally close the file.
                    reader.Close();
                }
                catch (Exception ex)
                {
                    activity.Items.Add(ex.Message);
                }
                
            }

            MessageBox.Show(contents);

            try
            {
                // Try to write the new file and tell the user it was written successfully.
                // If the file couldn't be writen then the user gets the error message.
                System.IO.StreamWriter writer = new System.IO.StreamWriter(filename);
                writer.Write(contents);
                writer.Flush();
                writer.Close();

                activity.Items.Add(filename + " was saved successfully");
            }
            catch (Exception ex)
            {
                activity.Items.Add(ex.Message);
            }
        }

    }
}
