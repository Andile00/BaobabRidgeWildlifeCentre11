using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BaobabRidgeWildlifeCentre.BusinessLogic;
using BaobabRidgeWildlifeCentre.DataLayer;
using BaobabRidgeWildlifeCentre.Models;

namespace BaobabRidgeWildlifeCentre
{
    public partial class MainForm : Form
    {
        private List<Animal> _animals = new List<Animal>();
        private bool _isEditMode = false;

        public MainForm()
        {
            InitializeComponent();

            // Wire up the events (so you don't have to do it in the designer)
            this.Load += MainForm_Load;
            btnAdd.Click += BtnAdd_Click;
            btnSaveChanges.Click += BtnSaveChanges_Click;
            btnClear.Click += BtnClear_Click;
            btnSearch.Click += BtnSearch_Click;
            btnDelete.Click += BtnDelete_Click;
            btnGenerateSummary.Click += BtnGenerateSummary_Click;
            dgvAnimals.CellClick += DgvAnimals_CellClick;
        }

        /// <summary>
        /// Runs when the form opens. Loads data from the file.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        /// <summary>
        /// Loads animals from the file, fills the grid, and tells user if any lines were skipped.
        /// </summary>
        private void LoadData()
        {
            var result = FileHandler.LoadAnimals();

            if (result.error != null)
            {
                MessageBox.Show(result.error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _animals = result.animals;
            RefreshGrid();

            if (result.skipped > 0)
            {
                MessageBox.Show($"{result.skipped} invalid line(s) were skipped.",
                    "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Rebuilds the grid from the _animals list. Prevents duplicate rows.
        /// </summary>
        private void RefreshGrid()
        {
            dgvAnimals.Rows.Clear();
            foreach (var a in _animals)
            {
                dgvAnimals.Rows.Add(a.AnimalId, a.Name, a.Species, a.Age, a.RecoveryScore, a.Status, a.HousingUnit);
            }
        }

        /// <summary>
        /// Validates and adds a new animal.
        /// </summary>
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            // Check if ID is unique
            string id = txtAnimalId.Text.Trim().ToUpper();
            if (_animals.Any(a => a.AnimalId == id))
            {
                MessageBox.Show("Animal ID already exists.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAnimalId.Focus();
                return;
            }

            // Use the single validation method from AnimalManager
            string error = AnimalManager.ValidateAll(id, txtName.Text, txtSpecies.Text, txtAge.Text, txtScore.Text);
            if (error != null)
            {
                MessageBox.Show(error, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // Stop saving if invalid
            }

            // Create new animal
            var newAnimal = new Animal
            {
                AnimalId = id,
                Name = txtName.Text.Trim(),
                Species = txtSpecies.Text.Trim(),
                Age = int.Parse(txtAge.Text.Trim()),
                RecoveryScore = int.Parse(txtScore.Text.Trim())
            };

            // Calculate status and housing
            AnimalManager.Classify(newAnimal);

            // Save to file
            string fileError = FileHandler.Append(newAnimal);
            if (fileError != null)
            {
                MessageBox.Show(fileError, "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update local list and grid
            _animals.Add(newAnimal);
            RefreshGrid();
            ClearForm();

            MessageBox.Show($"Added {newAnimal.AnimalId}.\nStatus: {newAnimal.Status}\nHousing: {newAnimal.HousingUnit}",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Loads the clicked row into the input boxes for editing.
        /// </summary>
        private void DgvAnimals_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string id = dgvAnimals.Rows[e.RowIndex].Cells[0].Value.ToString();
            LoadRecordForEdit(id);
        }

        /// <summary>
        /// Searches for an ID and loads it into the form.
        /// </summary>
        private void BtnSearch_Click(object sender, EventArgs e)
        {
            string search = txtSearchId.Text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(search))
            {
                MessageBox.Show("Enter an ID to search.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var found = _animals.FirstOrDefault(a => a.AnimalId == search);
            if (found == null)
            {
                MessageBox.Show("Animal not found.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            LoadRecordForEdit(found.AnimalId);
        }

        /// <summary>
        /// Fills the form with the animal's data and switches to Edit Mode.
        /// </summary>
        private void LoadRecordForEdit(string animalId)
        {
            var a = _animals.FirstOrDefault(x => x.AnimalId == animalId);
            if (a == null) return;

            txtAnimalId.Text = a.AnimalId;
            txtName.Text = a.Name;
            txtSpecies.Text = a.Species;
            txtAge.Text = a.Age.ToString();
            txtScore.Text = a.RecoveryScore.ToString();

            // Switch to Edit Mode
            _isEditMode = true;
            txtAnimalId.ReadOnly = true;
            btnAdd.Enabled = false;
            btnSaveChanges.Enabled = true;
        }

        /// <summary>
        /// Saves changes made to an existing animal.
        /// </summary>
        private void BtnSaveChanges_Click(object sender, EventArgs e)
        {
            if (!_isEditMode) return;

            // Validate (we don't check ID uniqueness here because it cannot change)
            string error = AnimalManager.ValidateAll(txtAnimalId.Text, txtName.Text, txtSpecies.Text, txtAge.Text, txtScore.Text);
            if (error != null)
            {
                MessageBox.Show(error, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Find and update the record
            var a = _animals.First(x => x.AnimalId == txtAnimalId.Text);
            a.Name = txtName.Text.Trim();
            a.Species = txtSpecies.Text.Trim();
            a.Age = int.Parse(txtAge.Text.Trim());
            a.RecoveryScore = int.Parse(txtScore.Text.Trim());

            // Recalculate status
            AnimalManager.Classify(a);

            // Save entire list back to file
            string fileError = FileHandler.SaveAll(_animals);
            if (fileError != null)
            {
                MessageBox.Show(fileError, "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RefreshGrid();
            ClearForm();
            MessageBox.Show("Record updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Clears the form and returns to Add Mode.
        /// </summary>
        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        /// <summary>
        /// Resets controls to Add Mode.
        /// </summary>
        private void ClearForm()
        {
            txtAnimalId.Clear();
            txtName.Clear();
            txtSpecies.Clear();
            txtAge.Clear();
            txtScore.Clear();
            txtSearchId.Clear();

            _isEditMode = false;
            txtAnimalId.ReadOnly = false;
            btnAdd.Enabled = true;
            btnSaveChanges.Enabled = false;
            txtAnimalId.Focus();
        }

        /// <summary>
        /// Deletes the selected animal.
        /// </summary>
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvAnimals.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row to delete.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string id = dgvAnimals.SelectedRows[0].Cells[0].Value.ToString();
            var a = _animals.FirstOrDefault(x => x.AnimalId == id);
            if (a == null) return;

            // Confirmation dialog
            var confirm = MessageBox.Show($"Delete {a.AnimalId} ({a.Name})? This cannot be undone.",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                _animals.Remove(a);

                string fileError = FileHandler.SaveAll(_animals);
                if (fileError != null)
                {
                    MessageBox.Show(fileError, "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                RefreshGrid();
                ClearForm();
                MessageBox.Show("Record deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Calculates summary data, updates labels, and writes summary.txt.
        /// </summary>
         private void BtnGenerateSummary_Click(object sender, EventArgs e)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;

            lblTotal.Text = $"Total animals: {_animals.Count}";
            lblAvgAge.Text = "Average age: " + (_animals.Count == 0 ? "N/A" : _animals.Average(a => a.Age).ToString("0.00", ci));
            lblAvgScore.Text = "Average score: " + (_animals.Count == 0 ? "N/A" : _animals.Average(a => a.RecoveryScore).ToString("0.00", ci));

            lblCritical.Text = $"Critical: {_animals.Count(a => a.Status == "Critical")}";
            lblSerious.Text = $"Serious: {_animals.Count(a => a.Status == "Serious")}";
            lblStable.Text = $"Stable: {_animals.Count(a => a.Status == "Stable")}";
            lblRecovering.Text = $"Recovering: {_animals.Count(a => a.Status == "Recovering")}";
            lblReleaseReady.Text = $"Release-Ready: {_animals.Count(a => a.Status == "Release-Ready")}";

            string error = FileHandler.WriteSummary(_animals);
            if (error != null)
            {
                MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("Summary generated and saved to summary.txt.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
