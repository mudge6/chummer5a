/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */
 using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
 using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
 using Chummer.Backend.Equipment;
 using Chummer.Skills;

namespace Chummer
{
    public partial class SpiritControl : UserControl
    {
        private readonly Spirit _objSpirit;

        // Events.
        public Action<object> ServicesOwedChanged;
        public Action<object> ForceChanged;
        public Action<object> BoundChanged;
        public Action<object> FetteredChanged;
        public Action<object> DeleteSpirit;
        public Action<object> FileNameChanged;

        #region Control Events
        public SpiritControl(Spirit objSpirit)
        {
            _objSpirit = objSpirit;
            InitializeComponent();
            LanguageManager.Load(GlobalOptions.Language, this);
        }

        private void nudServices_ValueChanged(object sender, EventArgs e)
        {
            // Raise the ServicesOwedChanged Event when the NumericUpDown's Value changes.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
            _objSpirit.ServicesOwed = decimal.ToInt32(nudServices.Value);
            ServicesOwedChanged(this);
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            // Raise the DeleteSpirit Event when the user has confirmed their desire to delete the Spirit.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
            DeleteSpirit(this);
        }

        private void nudForce_ValueChanged(object sender, EventArgs e)
        {
            // Raise the ForceChanged Event when the NumericUpDown's Value changes.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
            _objSpirit.Force = decimal.ToInt32(nudForce.Value);
            ForceChanged(this);
        }

        private void chkBound_CheckedChanged(object sender, EventArgs e)
        {
            // Raise the BoundChanged Event when the Checkbox's Checked status changes.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
            _objSpirit.Bound = chkBound.Checked;
            BoundChanged(this);
        }
        private void chkFettered_CheckedChanged(object sender, EventArgs e)
        {
            if (chkFettered.Checked)
            {
                //Only one Fettered spirit is permitted. 
                if (_objSpirit.CharacterObject.Spirits.Any(objSpirit => objSpirit.Fettered))
                {
                    chkFettered.Checked = false;
                    return;
                }
                ImprovementManager.CreateImprovement(_objSpirit.CharacterObject, "MAG", Improvement.ImprovementSource.SpiritFettering, "Spirit Fettering", Improvement.ImprovementType.Attribute, string.Empty, 0, 1, 0, 0, -1);
            }
            else
            {
                ImprovementManager.RemoveImprovements(_objSpirit.CharacterObject, Improvement.ImprovementSource.SpiritFettering, "Spirit Fettering");
            }
            _objSpirit.Fettered = chkFettered.Checked;

            // Raise the FetteredChanged Event when the Checkbox's Checked status changes.
            // The entire SpiritControl is passed as an argument so the handling event can evaluate its contents.
            FetteredChanged(this);
        }

        private void SpiritControl_Load(object sender, EventArgs e)
        {
            DoubleBuffered = true;
            nudForce.DataBindings.Add("Enabled", _objSpirit.CharacterObject, nameof(Character.Created), false,
                DataSourceUpdateMode.OnPropertyChanged);
            chkBound.DataBindings.Add("Checked", _objSpirit, nameof(_objSpirit.Bound), false,
                DataSourceUpdateMode.OnPropertyChanged);
            chkBound.DataBindings.Add("Enabled", _objSpirit.CharacterObject, nameof(Character.Created), false,
                DataSourceUpdateMode.OnPropertyChanged);
            cboSpiritName.DataBindings.Add("Text", _objSpirit, nameof(_objSpirit.Name), false,
                DataSourceUpdateMode.OnPropertyChanged);
            txtCritterName.DataBindings.Add("Text", _objSpirit, nameof(_objSpirit.CritterName), false,
                DataSourceUpdateMode.OnPropertyChanged);
            txtCritterName.DataBindings.Add("Enabled", _objSpirit, nameof(_objSpirit.NoLinkedCharacter), false,
                DataSourceUpdateMode.OnPropertyChanged);
            nudServices.DataBindings.Add("Value", _objSpirit, nameof(_objSpirit.ServicesOwed), false,
                DataSourceUpdateMode.OnPropertyChanged);
            nudForce.DataBindings.Add("Value", _objSpirit, nameof(_objSpirit.Force), false,
                DataSourceUpdateMode.OnPropertyChanged);
            chkFettered.DataBindings.Add("Checked", _objSpirit, nameof(_objSpirit.Fettered), false,
                DataSourceUpdateMode.OnPropertyChanged);
            Width = cmdDelete.Left + cmdDelete.Width;
        }

        private void cboSpiritName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboSpiritName.SelectedValue != null)
                _objSpirit.Name = cboSpiritName.SelectedValue.ToString();
            ForceChanged(this);
        }

        private void txtCritterName_TextChanged(object sender, EventArgs e)
        {
            ForceChanged(this);
        }

        private void tsContactOpen_Click(object sender, EventArgs e)
        {
            if (_objSpirit.LinkedCharacter != null)
            {
                Character objOpenCharacter = GlobalOptions.MainForm.OpenCharacters.FirstOrDefault(x => x == _objSpirit.LinkedCharacter);
                Cursor = Cursors.WaitCursor;
                if (objOpenCharacter == null || !GlobalOptions.MainForm.SwitchToOpenCharacter(objOpenCharacter, true))
                {
                    objOpenCharacter = frmMain.LoadCharacter(_objSpirit.LinkedCharacter.FileName);
                    GlobalOptions.MainForm.OpenCharacter(objOpenCharacter);
                }
                Cursor = Cursors.Default;
            }
            else
            {
                bool blnUseRelative = false;

                // Make sure the file still exists before attempting to load it.
                if (!File.Exists(_objSpirit.FileName))
                {
                    bool blnError = false;
                    // If the file doesn't exist, use the relative path if one is available.
                    if (string.IsNullOrEmpty(_objSpirit.RelativeFileName))
                        blnError = true;
                    else if (!File.Exists(Path.GetFullPath(_objSpirit.RelativeFileName)))
                        blnError = true;
                    else
                        blnUseRelative = true;

                    if (blnError)
                    {
                        MessageBox.Show(LanguageManager.GetString("Message_FileNotFound").Replace("{0}", _objSpirit.FileName), LanguageManager.GetString("MessageTitle_FileNotFound"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                string strFile = blnUseRelative ? Path.GetFullPath(_objSpirit.RelativeFileName) : _objSpirit.FileName;
                System.Diagnostics.Process.Start(strFile);
            }
        }

        private void tsRemoveCharacter_Click(object sender, EventArgs e)
        {
            // Remove the file association from the Contact.
            if (MessageBox.Show(LanguageManager.GetString("Message_RemoveCharacterAssociation"), LanguageManager.GetString("MessageTitle_RemoveCharacterAssociation"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _objSpirit.FileName = string.Empty;
                _objSpirit.RelativeFileName = string.Empty;
                if (_objSpirit.EntityType ==  SpiritType.Spirit)
                    tipTooltip.SetToolTip(imgLink, LanguageManager.GetString("Tip_Spirit_LinkSpirit"));
                else
                    tipTooltip.SetToolTip(imgLink, LanguageManager.GetString("Tip_Sprite_LinkSprite"));

                // Set the relative path.
                Uri uriApplication = new Uri(@Application.StartupPath);
                Uri uriFile = new Uri(@_objSpirit.FileName);
                Uri uriRelative = uriApplication.MakeRelativeUri(uriFile);
                _objSpirit.RelativeFileName = "../" + uriRelative.ToString();

                FileNameChanged(this);
            }
        }

        private void tsAttachCharacter_Click(object sender, EventArgs e)
        {
            // Prompt the user to select a save file to associate with this Contact.
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Chummer5 Files (*.chum5)|*.chum5|All Files (*.*)|*.*";
            if (!string.IsNullOrEmpty(_objSpirit.FileName) && File.Exists(_objSpirit.FileName))
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(_objSpirit.FileName);
                openFileDialog.FileName = Path.GetFileName(_objSpirit.FileName);
            }
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                _objSpirit.FileName = openFileDialog.FileName;
                if (_objSpirit.EntityType == SpiritType.Spirit)
                    tipTooltip.SetToolTip(imgLink, LanguageManager.GetString("Tip_Spirit_OpenFile"));
                else
                    tipTooltip.SetToolTip(imgLink, LanguageManager.GetString("Tip_Sprite_OpenFile"));
                FileNameChanged(this);
            }
        }

        private void tsCreateCharacter_Click(object sender, EventArgs e)
        {
            string strSpiritName = cboSpiritName.SelectedValue?.ToString();
            if (string.IsNullOrEmpty(strSpiritName))
            {
                MessageBox.Show(LanguageManager.GetString("Message_SelectCritterType"), LanguageManager.GetString("MessageTitle_SelectCritterType"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CreateCritter(strSpiritName, decimal.ToInt32(nudForce.Value));
        }

        private void imgLink_Click(object sender, EventArgs e)
        {
            // Determine which options should be shown based on the FileName value.
            if (!string.IsNullOrEmpty(_objSpirit.FileName))
            {
                tsAttachCharacter.Visible = false;
                tsCreateCharacter.Visible = false;
                tsContactOpen.Visible = true;
                tsRemoveCharacter.Visible = true;
            }
            else
            {
                tsAttachCharacter.Visible = true;
                tsCreateCharacter.Visible = true;
                tsContactOpen.Visible = false;
                tsRemoveCharacter.Visible = false;
            }
            cmsSpirit.Show(imgLink, imgLink.Left - 646, imgLink.Top);
        }

        private void imgNotes_Click(object sender, EventArgs e)
        {
            frmNotes frmSpritNotes = new frmNotes();
            frmSpritNotes.Notes = _objSpirit.Notes;
            frmSpritNotes.ShowDialog(this);

            if (frmSpritNotes.DialogResult == DialogResult.OK)
                _objSpirit.Notes = frmSpritNotes.Notes;

            string strTooltip = string.Empty;
            if (_objSpirit.EntityType == SpiritType.Spirit)
                strTooltip = LanguageManager.GetString("Tip_Spirit_EditNotes");
            else
                strTooltip = LanguageManager.GetString("Tip_Sprite_EditNotes");
            if (!string.IsNullOrEmpty(_objSpirit.Notes))
                strTooltip += "\n\n" + _objSpirit.Notes;
            tipTooltip.SetToolTip(imgNotes, CommonFunctions.WordWrap(strTooltip, 100));
        }

        private void ContextMenu_Opening(object sender, CancelEventArgs e)
        {
            foreach (ToolStripItem objItem in ((ContextMenuStrip)sender).Items)
            {
                if (objItem.Tag != null)
                {
                    objItem.Text = LanguageManager.GetString(objItem.Tag.ToString());
                }
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Spirit object this is linked to.
        /// </summary>
        public Spirit SpiritObject
        {
            get
            {
                return _objSpirit;
            }
        }

        /// <summary>
        /// Spirit Metatype name.
        /// </summary>
        public string SpiritName
        {
            get
            {
                return _objSpirit.Name;
            }
        }

        /// <summary>
        /// Spirit name.
        /// </summary>
        public string CritterName
        {
            get
            {
                return _objSpirit.CritterName;
            }
        }

        /// <summary>
        /// Indicates if this is a Spirit or Sprite. For labeling purposes only.
        /// </summary>
        public SpiritType EntityType
        {
            get
            {
                return _objSpirit.EntityType;
            }
            set
            {
                _objSpirit.EntityType = value;
                if (value == SpiritType.Spirit)
                {
                    lblForce.Text = LanguageManager.GetString("Label_Spirit_Force");
                    chkBound.Text = LanguageManager.GetString("Checkbox_Spirit_Bound");
                    if (!string.IsNullOrEmpty(_objSpirit.FileName))
                        tipTooltip.SetToolTip(imgLink, LanguageManager.GetString("Tip_Spirit_OpenFile"));
                    else
                        tipTooltip.SetToolTip(imgLink, LanguageManager.GetString("Tip_Spirit_LinkSpirit"));

                    string strTooltip = LanguageManager.GetString("Tip_Spirit_EditNotes");
                    if (!string.IsNullOrEmpty(_objSpirit.Notes))
                        strTooltip += "\n\n" + _objSpirit.Notes;
                    tipTooltip.SetToolTip(imgNotes, CommonFunctions.WordWrap(strTooltip, 100));
                }
                else
                {
                    lblForce.Text = LanguageManager.GetString("Label_Sprite_Rating");
                    chkBound.Text = LanguageManager.GetString("Label_Sprite_Registered");
                    if (!string.IsNullOrEmpty(_objSpirit.FileName))
                        tipTooltip.SetToolTip(imgLink, "Open the linked Sprite save file.");
                    else
                        tipTooltip.SetToolTip(imgLink, "Link this Sprite to a Chummer save file.");

                    string strTooltip = LanguageManager.GetString("Tip_Sprite_EditNotes");
                    if (!string.IsNullOrEmpty(_objSpirit.Notes))
                        strTooltip += "\n\n" + _objSpirit.Notes;
                    tipTooltip.SetToolTip(imgNotes, CommonFunctions.WordWrap(strTooltip, 100));
                }
            }
        }

        /// <summary>
        /// Services owed.
        /// </summary>
        public int ServicesOwed
        {
            get
            {
                return _objSpirit.ServicesOwed;
            }
            set
            {
                _objSpirit.ServicesOwed = value;
            }
        }

        /// <summary>
        /// Force of the Spirit.
        /// </summary>
        public int Force
        {
            get
            {
                return _objSpirit.Force;
            }
            set
            {
                _objSpirit.Force = value;
            }
        }

        /// <summary>
        /// Maximum Force of the Spirit.
        /// </summary>
        public int ForceMaximum
        {
            get
            {
                return decimal.ToInt32(nudForce.Maximum);
            }
            set
            {
                nudForce.Maximum = value;
            }
        }

        /// <summary>
        /// Whether or not the Spirit is Bound.
        /// </summary>
        public bool Bound
        {
            get
            {
                return _objSpirit.Bound;
            }
            set
            {
                _objSpirit.Bound = value;
            }
        }

        /// <summary>
        /// Whether or not the Spirit is Fettered.
        /// </summary>
        public bool Fettered
        {
            get
            {
                return _objSpirit.Fettered;
            }
            set
            {
                _objSpirit.Fettered = value;
            }
        }
        #endregion

        #region Methods
        // Rebuild the list of Spirits/Sprites based on the character's selected Tradition/Stream.
        public void RebuildSpiritList(string strTradition)
        {
            if (string.IsNullOrEmpty(strTradition))
            {
                return;
            }
            string strCurrentValue = _objSpirit.Name;
            if (cboSpiritName.SelectedValue != null)
                strCurrentValue = cboSpiritName.SelectedValue.ToString();

            XmlDocument objXmlDocument = null;
            if (_objSpirit.EntityType == SpiritType.Spirit)
                objXmlDocument = XmlManager.Load("traditions.xml");
            else
                objXmlDocument = XmlManager.Load("streams.xml");
            XmlDocument objXmlCritterDocument = XmlManager.Load("critters.xml");

            HashSet<string> lstLimitCategories = new HashSet<string>();
            foreach (Improvement improvement in _objSpirit.CharacterObject.Improvements.Where(improvement => improvement.ImproveType == Improvement.ImprovementType.LimitSpiritCategory))
            {
                lstLimitCategories.Add(improvement.ImprovedName);
            }

            List<ListItem> lstCritters = new List<ListItem>();
            if (strTradition == "Custom")
            {
                if (lstLimitCategories.Count == 0 || lstLimitCategories.Contains(_objSpirit.CharacterObject.SpiritCombat))
                {
                    ListItem objCombat = new ListItem();
                    objCombat.Value = _objSpirit.CharacterObject.SpiritCombat;
                    XmlNode objXmlCritterNode = objXmlDocument.SelectSingleNode("/chummer/spirits/spirit[name = \"" + _objSpirit.CharacterObject.SpiritCombat + "\"]");
                    objCombat.Name = objXmlCritterNode?["translate"]?.InnerText ?? _objSpirit.CharacterObject.SpiritCombat;
                    lstCritters.Add(objCombat);
                }

                if (lstLimitCategories.Count == 0 || lstLimitCategories.Contains(_objSpirit.CharacterObject.SpiritDetection))
                {
                    ListItem objDetection = new ListItem();
                    objDetection.Value = _objSpirit.CharacterObject.SpiritDetection;
                    XmlNode objXmlCritterNode = objXmlDocument.SelectSingleNode("/chummer/spirits/spirit[name = \"" + _objSpirit.CharacterObject.SpiritDetection + "\"]");
                    objDetection.Name = objXmlCritterNode?["translate"]?.InnerText ?? _objSpirit.CharacterObject.SpiritDetection;
                    lstCritters.Add(objDetection);
                }

                if (lstLimitCategories.Count == 0 || lstLimitCategories.Contains(_objSpirit.CharacterObject.SpiritHealth))
                {
                    ListItem objHealth = new ListItem();
                    objHealth.Value = _objSpirit.CharacterObject.SpiritHealth;
                    XmlNode objXmlCritterNode = objXmlDocument.SelectSingleNode("/chummer/spirits/spirit[name = \"" + _objSpirit.CharacterObject.SpiritHealth + "\"]");
                    objHealth.Name = objXmlCritterNode?["translate"]?.InnerText ?? _objSpirit.CharacterObject.SpiritHealth;
                    lstCritters.Add(objHealth);
                }

                if (lstLimitCategories.Count == 0 || lstLimitCategories.Contains(_objSpirit.CharacterObject.SpiritIllusion))
                {
                    ListItem objIllusion = new ListItem();
                    objIllusion.Value = _objSpirit.CharacterObject.SpiritIllusion;
                    XmlNode objXmlCritterNode = objXmlDocument.SelectSingleNode("/chummer/spirits/spirit[name = \"" + _objSpirit.CharacterObject.SpiritIllusion + "\"]");
                    objIllusion.Name = objXmlCritterNode?["translate"]?.InnerText ?? _objSpirit.CharacterObject.SpiritIllusion;
                    lstCritters.Add(objIllusion);
                }

                if (lstLimitCategories.Count == 0 || lstLimitCategories.Contains(_objSpirit.CharacterObject.SpiritManipulation))
                {
                    ListItem objManipulation = new ListItem();
                    objManipulation.Value = _objSpirit.CharacterObject.SpiritManipulation;
                    XmlNode objXmlCritterNode = objXmlDocument.SelectSingleNode("/chummer/spirits/spirit[name = \"" + _objSpirit.CharacterObject.SpiritManipulation + "\"]");
                    objManipulation.Name = objXmlCritterNode?["translate"]?.InnerText ?? _objSpirit.CharacterObject.SpiritManipulation;
                    lstCritters.Add(objManipulation);
                }
            }
            else
            {
                foreach (XmlNode objXmlSpirit in objXmlDocument.SelectSingleNode("/chummer/traditions/tradition[name = \"" + strTradition + "\"]/spirits").ChildNodes)
                {
                    string strSpiritName = objXmlSpirit.InnerText;
                    if (lstLimitCategories.Count == 0 || lstLimitCategories.Contains(strSpiritName))
                    {
                        ListItem objItem = new ListItem();
                        objItem.Value = strSpiritName;
                        XmlNode objXmlCritterNode = objXmlDocument.SelectSingleNode("/chummer/spirits/spirit[name = \"" + strSpiritName + "\"]");
                        objItem.Name = objXmlCritterNode?["translate"]?.InnerText ?? strSpiritName;

                        lstCritters.Add(objItem);
                    }
                }
            }

            if (_objSpirit.CharacterObject.RESEnabled)
            {
                // Add any additional Sprites the character has Access to through Sprite Link.
                foreach (Improvement objImprovement in _objSpirit.CharacterObject.Improvements)
                {
                    if (objImprovement.ImproveType == Improvement.ImprovementType.AddSprite)
                    {
                        ListItem objItem = new ListItem();
                        objItem.Value = objImprovement.ImprovedName;
                        objItem.Name = objImprovement.ImprovedName;
                        lstCritters.Add(objItem);
                    }
                }
            }

            //Add Ally Spirit to MAG-enabled traditions.
            if (_objSpirit.CharacterObject.MAGEnabled)
            {
                ListItem objItem = new ListItem();
                objItem.Value = "Ally Spirit";
                XmlNode objXmlCritterNode = objXmlCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objItem.Value + "\"]");
                if (objXmlCritterNode["translate"] != null)
                    objItem.Name = objXmlCritterNode["translate"].InnerText;
                else
                    objItem.Name = objItem.Value;
                lstCritters.Add(objItem);
            }

            cboSpiritName.BeginUpdate();
            cboSpiritName.DisplayMember = "Name";
            cboSpiritName.ValueMember = "Value";
            cboSpiritName.DataSource = lstCritters;

            // Set the control back to its original value.
            cboSpiritName.SelectedValue = strCurrentValue;
            cboSpiritName.EndUpdate();
        }

        /// <summary>
        /// Create a Critter, put them into Career Mode, link them, and open the newly-created Critter.
        /// </summary>
        /// <param name="strCritterName">Name of the Critter's Metatype.</param>
        /// <param name="intForce">Critter's Force.</param>
        private void CreateCritter(string strCritterName, int intForce)
        {
            // The Critter should use the same settings file as the character.
            Character objCharacter = new Character();
            objCharacter.SettingsFile = _objSpirit.CharacterObject.SettingsFile;

            // Override the defaults for the setting.
            objCharacter.IgnoreRules = true;
            objCharacter.IsCritter = true;
            objCharacter.BuildMethod = CharacterBuildMethod.Karma;
            objCharacter.BuildPoints = 0;

            if (!string.IsNullOrEmpty(txtCritterName.Text))
                objCharacter.Name = txtCritterName.Text;

            // Ask the user to select a filename for the new character.
            string strForce = LanguageManager.GetString("String_Force");
            if (_objSpirit.EntityType == SpiritType.Sprite)
                strForce = LanguageManager.GetString("String_Rating");
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Chummer5 Files (*.chum5)|*.chum5|All Files (*.*)|*.*";
            saveFileDialog.FileName = strCritterName + " (" + strForce + " " + _objSpirit.Force.ToString() + ").chum5";
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string strFileName = saveFileDialog.FileName;
                objCharacter.FileName = strFileName;
            }
            else
            {
                objCharacter.Dispose();
                return;
            }

            Cursor = Cursors.WaitCursor;

            // Code from frmMetatype.
            XmlDocument objXmlDocument = XmlManager.Load("critters.xml");

            XmlNode objXmlMetatype = objXmlDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + strCritterName + "\"]");

            // If the Critter could not be found, show an error and get out of here.
            if (objXmlMetatype == null)
            {
                MessageBox.Show(LanguageManager.GetString("Message_UnknownCritterType").Replace("{0}", strCritterName), LanguageManager.GetString("MessageTitle_SelectCritterType"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Set Metatype information.
            if (strCritterName == "Ally Spirit")
            {
                objCharacter.BOD.AssignLimits(ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["bodmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["bodaug"].InnerText, intForce, 0));
                objCharacter.AGI.AssignLimits(ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["agimax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["agiaug"].InnerText, intForce, 0));
                objCharacter.REA.AssignLimits(ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["reamax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["reaaug"].InnerText, intForce, 0));
                objCharacter.STR.AssignLimits(ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["strmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["straug"].InnerText, intForce, 0));
                objCharacter.CHA.AssignLimits(ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["chamax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["chaaug"].InnerText, intForce, 0));
                objCharacter.INT.AssignLimits(ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["intmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["intaug"].InnerText, intForce, 0));
                objCharacter.LOG.AssignLimits(ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["logmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["logaug"].InnerText, intForce, 0));
                objCharacter.WIL.AssignLimits(ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["wilmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["wilaug"].InnerText, intForce, 0));
                objCharacter.MAG.AssignLimits(ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["magmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["magaug"].InnerText, intForce, 0));
                objCharacter.RES.AssignLimits(ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["resmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["resaug"].InnerText, intForce, 0));
                objCharacter.EDG.AssignLimits(ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["edgmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["edgaug"].InnerText, intForce, 0));
                objCharacter.ESS.AssignLimits(ExpressionToString(objXmlMetatype["essmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essaug"].InnerText, intForce, 0));
            }
            else
            {
                int intMinModifier = -3;
                objCharacter.BOD.AssignLimits(ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 3));
                objCharacter.AGI.AssignLimits(ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 3));
                objCharacter.REA.AssignLimits(ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 3));
                objCharacter.STR.AssignLimits(ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 3));
                objCharacter.CHA.AssignLimits(ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 3));
                objCharacter.INT.AssignLimits(ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 3));
                objCharacter.LOG.AssignLimits(ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 3));
                objCharacter.WIL.AssignLimits(ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 3));
                objCharacter.MAG.AssignLimits(ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 3));
                objCharacter.RES.AssignLimits(ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 3));
                objCharacter.EDG.AssignLimits(ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 3));
                objCharacter.ESS.AssignLimits(ExpressionToString(objXmlMetatype["essmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essaug"].InnerText, intForce, 0));
            }

            // If we're working with a Critter, set the Attributes to their default values.
            objCharacter.BOD.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 0));
            objCharacter.AGI.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 0));
            objCharacter.REA.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 0));
            objCharacter.STR.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 0));
            objCharacter.CHA.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 0));
            objCharacter.INT.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 0));
            objCharacter.LOG.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 0));
            objCharacter.WIL.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 0));
            objCharacter.MAG.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 0));
            objCharacter.RES.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 0));
            objCharacter.EDG.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 0));
            objCharacter.ESS.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["essmax"].InnerText, intForce, 0));

            // Sprites can never have Physical Attributes or WIL.
            if (objXmlMetatype["category"].InnerText.EndsWith("Sprite"))
            {
                objCharacter.BOD.AssignLimits("0", "0", "0");
                objCharacter.AGI.AssignLimits("0", "0", "0");
                objCharacter.REA.AssignLimits("0", "0", "0");
                objCharacter.STR.AssignLimits("0", "0", "0");
                objCharacter.WIL.AssignLimits("0", "0", "0");
            }

            objCharacter.Metatype = strCritterName;
            objCharacter.MetatypeCategory = objXmlMetatype["category"].InnerText;
            objCharacter.Metavariant = string.Empty;
            objCharacter.MetatypeBP = 0;

            if (objXmlMetatype["movement"] != null)
                objCharacter.Movement = objXmlMetatype["movement"].InnerText;
            // Load the Qualities file.
            XmlDocument objXmlQualityDocument = XmlManager.Load("qualities.xml");

            // Determine if the Metatype has any bonuses.
            if (objXmlMetatype.InnerXml.Contains("bonus"))
                ImprovementManager.CreateImprovements(objCharacter, Improvement.ImprovementSource.Metatype, strCritterName, objXmlMetatype.SelectSingleNode("bonus"), false, 1, strCritterName);

            // Create the Qualities that come with the Metatype.
            foreach (XmlNode objXmlQualityItem in objXmlMetatype.SelectNodes("qualities/positive/quality"))
            {
                XmlNode objXmlQuality = objXmlQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQualityItem.InnerText + "\"]");
                TreeNode objNode = new TreeNode();
                List<Weapon> objWeapons = new List<Weapon>();
                List<TreeNode> objWeaponNodes = new List<TreeNode>();
                Quality objQuality = new Quality(objCharacter);
                string strForceValue = string.Empty;
                if (objXmlQualityItem.Attributes["select"] != null)
                    strForceValue = objXmlQualityItem.Attributes["select"].InnerText;
                QualitySource objSource = QualitySource.Metatype;
                if (objXmlQualityItem.Attributes["removable"]?.InnerText == bool.TrueString)
                    objSource = QualitySource.MetatypeRemovable;
                objQuality.Create(objXmlQuality, objCharacter, objSource, objNode, objWeapons, objWeaponNodes, strForceValue);
                objCharacter.Qualities.Add(objQuality);

                // Add any created Weapons to the character.
                foreach (Weapon objWeapon in objWeapons)
                    objCharacter.Weapons.Add(objWeapon);
            }
            foreach (XmlNode objXmlQualityItem in objXmlMetatype.SelectNodes("qualities/negative/quality"))
            {
                XmlNode objXmlQuality = objXmlQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQualityItem.InnerText + "\"]");
                TreeNode objNode = new TreeNode();
                List<Weapon> objWeapons = new List<Weapon>();
                List<TreeNode> objWeaponNodes = new List<TreeNode>();
                Quality objQuality = new Quality(objCharacter);
                string strForceValue = string.Empty;
                if (objXmlQualityItem.Attributes["select"] != null)
                    strForceValue = objXmlQualityItem.Attributes["select"].InnerText;
                QualitySource objSource = QualitySource.Metatype;
                if (objXmlQualityItem.Attributes["removable"]?.InnerText == bool.TrueString)
                    objSource = QualitySource.MetatypeRemovable;
                objQuality.Create(objXmlQuality, objCharacter, objSource, objNode, objWeapons, objWeaponNodes, strForceValue);
                objCharacter.Qualities.Add(objQuality);

                // Add any created Weapons to the character.
                foreach (Weapon objWeapon in objWeapons)
                    objCharacter.Weapons.Add(objWeapon);
            }

            // Add any Critter Powers the Metatype/Critter should have.
            XmlNode objXmlCritter = objXmlDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objCharacter.Metatype + "\"]");

            objXmlDocument = XmlManager.Load("critterpowers.xml");
            foreach (XmlNode objXmlPower in objXmlCritter.SelectNodes("powers/power"))
            {
                XmlNode objXmlCritterPower = objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + objXmlPower.InnerText + "\"]");
                TreeNode objNode = new TreeNode();
                CritterPower objPower = new CritterPower(objCharacter);
                string strForcedValue = string.Empty;
                int intRating = 0;

                if (objXmlPower.Attributes["rating"] != null)
                    intRating = Convert.ToInt32(objXmlPower.Attributes["rating"].InnerText);
                if (objXmlPower.Attributes["select"] != null)
                    strForcedValue = objXmlPower.Attributes["select"].InnerText;

                objPower.Create(objXmlCritterPower, objNode, intRating, strForcedValue);
                objCharacter.CritterPowers.Add(objPower);
            }

            if (objXmlCritter["optionalpowers"] != null)
            {
                //For every 3 full points of Force a spirit has, it may gain one Optional Power. 
                for (int i = intForce - 3; i >= 0; i -= 3)
                {
                    XmlDocument objDummyDocument = new XmlDocument();
                    XmlNode bonusNode = objDummyDocument.CreateNode(XmlNodeType.Element, "bonus", null);
                    objDummyDocument.AppendChild(bonusNode);
                    XmlNode powerNode = objDummyDocument.ImportNode(objXmlMetatype["optionalpowers"].CloneNode(true), true);
                    objDummyDocument.ImportNode(powerNode, true);
                    bonusNode.AppendChild(powerNode);
                    ImprovementManager.CreateImprovements(objCharacter, Improvement.ImprovementSource.Metatype, objCharacter.Metatype, bonusNode, false, 1, objCharacter.Metatype);
                }
            }
            // Add any Complex Forms the Critter comes with (typically Sprites)
            XmlDocument objXmlProgramDocument = XmlManager.Load("complexforms.xml");
            foreach (XmlNode objXmlComplexForm in objXmlCritter.SelectNodes("complexforms/complexform"))
            {
                string strForceValue = string.Empty;
                if (objXmlComplexForm.Attributes["select"] != null)
                    strForceValue = objXmlComplexForm.Attributes["select"].InnerText;
                XmlNode objXmlProgram = objXmlProgramDocument.SelectSingleNode("/chummer/complexforms/complexform[name = \"" + objXmlComplexForm.InnerText + "\"]");
                TreeNode objNode = new TreeNode();
                ComplexForm objProgram = new ComplexForm(objCharacter);
                objProgram.Create(objXmlProgram, objNode, null, strForceValue);
                objCharacter.ComplexForms.Add(objProgram);
            }

            // Add any Gear the Critter comes with (typically Programs for A.I.s)
            XmlDocument objXmlGearDocument = XmlManager.Load("gear.xml");
            foreach (XmlNode objXmlGear in objXmlCritter.SelectNodes("gears/gear"))
            {
                int intRating = 0;
                if (objXmlGear.Attributes["rating"] != null)
                    intRating = Convert.ToInt32(ExpressionToString(objXmlGear.Attributes["rating"].InnerText, decimal.ToInt32(nudForce.Value), 0));
                string strForceValue = string.Empty;
                if (objXmlGear.Attributes["select"] != null)
                    strForceValue = objXmlGear.Attributes["select"].InnerText;
                XmlNode objXmlGearItem = objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlGear.InnerText + "\"]");
                TreeNode objNode = new TreeNode();
                Gear objGear = new Gear(objCharacter);
                List<Weapon> lstWeapons = new List<Weapon>();
                List<TreeNode> lstWeaponNodes = new List<TreeNode>();
                objGear.Create(objXmlGearItem, objNode, intRating, lstWeapons, lstWeaponNodes, strForceValue);
                objGear.Cost = "0";
                objCharacter.Gear.Add(objGear);
            }

            // Add the Unarmed Attack Weapon to the character.
            objXmlDocument = XmlManager.Load("weapons.xml");
            XmlNode objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"Unarmed Attack\"]");
            if (objXmlWeapon != null)
            {
                Weapon objWeapon = new Weapon(objCharacter);
                objWeapon.Create(objXmlWeapon, null, null, null, objCharacter.Weapons);
                objWeapon.ParentID = Guid.NewGuid().ToString(); // Unarmed Attack can never be removed
                objCharacter.Weapons.Add(objWeapon);
            }

            objCharacter.Alias = strCritterName;
            objCharacter.Created = true;
            if (!objCharacter.Save())
            {
                Cursor = Cursors.Default;
                objCharacter.Dispose();
                return;
            }

            string strOpenFile = objCharacter.FileName;
            objCharacter.Dispose();
            objCharacter = null;

            // Link the newly-created Critter to the Spirit.
            _objSpirit.FileName = strOpenFile;
            if (_objSpirit.EntityType == SpiritType.Spirit)
                tipTooltip.SetToolTip(imgLink, LanguageManager.GetString("Tip_Spirit_OpenFile"));
            else
                tipTooltip.SetToolTip(imgLink, LanguageManager.GetString("Tip_Sprite_OpenFile"));
            FileNameChanged(this);
            
            Character objOpenCharacter = frmMain.LoadCharacter(strOpenFile);
            Cursor = Cursors.Default;
            GlobalOptions.MainForm.OpenCharacter(objOpenCharacter);
        }

        /// <summary>
        /// Convert Force, 1D6, or 2D6 into a usable value.
        /// </summary>
        /// <param name="strIn">Expression to convert.</param>
        /// <param name="intForce">Force value to use.</param>
        /// <param name="intOffset">Dice offset.</param>
        /// <returns></returns>
        public string ExpressionToString(string strIn, int intForce, int intOffset)
        {
            int intValue = 0;
            string strForce = intForce.ToString();
            // This statement is wrapped in a try/catch since trying 1 div 2 results in an error with XSLT.
            try
            {
                intValue = Convert.ToInt32(Math.Ceiling((double)CommonFunctions.EvaluateInvariantXPath(strIn.Replace("/", " div ").Replace("F", strForce).Replace("1D6", strForce).Replace("2D6", strForce))));
            }
            catch (XPathException) { }
            catch (OverflowException) { } // Result is text and not a double
            catch (InvalidCastException) { } // Result is text and not a double
            intValue += intOffset;
            if (intForce > 0)
            {
                if (intValue < 1)
                    intValue = 1;
            }
            else
            {
                if (intValue < 0)
                    intValue = 0;
            }
            return intValue.ToString();
        }
        #endregion

    }
}
