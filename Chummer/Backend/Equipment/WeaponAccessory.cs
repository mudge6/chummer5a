using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

namespace Chummer.Backend.Equipment
{
    /// <summary>
    /// Weapon Accessory.
    /// </summary>
    public class WeaponAccessory : INamedItemWithGuidAndNode
    {
        private Guid _guiID = Guid.Empty;
        private readonly Character _objCharacter;
        private XmlNode _nodAllowGear;
        private List<Gear> _lstGear = new List<Gear>();
        private Weapon _objParent;
        private string _strName = string.Empty;
        private string _strMount = string.Empty;
        private string _strExtraMount = string.Empty;
        private string _strRC = string.Empty;
        private string _strDamage = string.Empty;
        private string _strDamageType = string.Empty;
        private string _strDamageReplace = string.Empty;
        private string _strFireMode = string.Empty;
        private string _strFireModeReplace = string.Empty;
        private string _strAPReplace = string.Empty;
        private string _strAP = string.Empty;
        private string _strConceal = string.Empty;
        private string _strAvail = string.Empty;
        private string _strCost = string.Empty;
        private string _strSource = string.Empty;
        private string _strPage = string.Empty;
        private string _strNotes = string.Empty;
        private string _strAltName = string.Empty;
        private string _strAltPage = string.Empty;
        private string _strDicePool = string.Empty;
        private int _intAccuracy = 0;
        private int _intRating = 0;
        private int _intRCGroup = 0;
        private int _intAmmoSlots = 0;
        private bool _blnDeployable = false;
        private bool _blnDiscountCost = false;
        private bool _blnBlackMarketDiscount = false;
        private bool _blnIncludedInWeapon = false;
        private bool _blnInstalled = true;
        private int _intAccessoryCostMultiplier = 1;
        private string _strExtra = string.Empty;
        private int _intRangeBonus = 0;
        private int _intSuppressive = 0;
        private int _intFullBurst = 0;
        private string _strAddMode = string.Empty;
        private string _strAmmoReplace = string.Empty;
        private int _intAmmoBonus = 0;

        #region Constructor, Create, Save, Load, and Print Methods
        public WeaponAccessory(Character objCharacter)
        {
            // Create the GUID for the new Weapon.
            _guiID = Guid.NewGuid();
            _objCharacter = objCharacter;
        }

        /// Create a Weapon Accessory from an XmlNode and return the TreeNodes for it.
        /// <param name="objXmlAccessory">XmlNode to create the object from.</param>
        /// <param name="objNode">TreeNode to populate a TreeView.</param>
        /// <param name="strMount">Mount slot that the Weapon Accessory will consume.</param>
        /// <param name="intRating">Rating of the Weapon Accessory.</param>
        public void Create(XmlNode objXmlAccessory, TreeNode objNode, Tuple<string, string> strMount, int intRating, ContextMenuStrip cmsAccessoryGear, bool blnSkipCost = false, bool blnCreateChildren = true, bool blnCreateImprovements = true)
        {
            if (objXmlAccessory.TryGetStringFieldQuickly("name", ref _strName))
                _objCachedMyXmlNode = null;
            _strMount = strMount.Item1;
            _strExtraMount = strMount.Item2;
            _intRating = intRating;
            objXmlAccessory.TryGetStringFieldQuickly("avail", ref _strAvail);
            // Check for a Variable Cost.
            if (blnSkipCost)
                _strCost = "0";
            else if (objXmlAccessory["cost"] != null)
            {
                _strCost = objXmlAccessory["cost"].InnerText;
                if (_strCost.StartsWith("Variable"))
                {
                    decimal decMin = 0;
                    decimal decMax = decimal.MaxValue;
                    string strCost = _strCost.TrimStart("Variable", true).Trim("()".ToCharArray());
                    if (strCost.Contains('-'))
                    {
                        string[] strValues = strCost.Split('-');
                        decMin = Convert.ToDecimal(strValues[0], GlobalOptions.InvariantCultureInfo);
                        decMax = Convert.ToDecimal(strValues[1], GlobalOptions.InvariantCultureInfo);
                    }
                    else
                        decMin = Convert.ToDecimal(strCost.FastEscape('+'), GlobalOptions.InvariantCultureInfo);

                    if (decMin != 0 || decMax != decimal.MaxValue)
                    {
                        string strNuyenFormat = _objCharacter.Options.NuyenFormat;
                        int intDecimalPlaces = strNuyenFormat.IndexOf('.');
                        if (intDecimalPlaces == -1)
                            intDecimalPlaces = 0;
                        else
                            intDecimalPlaces = strNuyenFormat.Length - intDecimalPlaces - 1;
                        frmSelectNumber frmPickNumber = new frmSelectNumber(intDecimalPlaces);
                        if (decMax > 1000000)
                            decMax = 1000000;
                        frmPickNumber.Minimum = decMin;
                        frmPickNumber.Maximum = decMax;
                        frmPickNumber.Description = LanguageManager.GetString("String_SelectVariableCost").Replace("{0}", DisplayNameShort);
                        frmPickNumber.AllowCancel = false;
                        frmPickNumber.ShowDialog();
                        _strCost = frmPickNumber.SelectedValue.ToString();
                    }
                }
            }

            objXmlAccessory.TryGetStringFieldQuickly("source", ref _strSource);
            objXmlAccessory.TryGetStringFieldQuickly("page", ref _strPage);
            _nodAllowGear = objXmlAccessory["allowgear"];
            objXmlAccessory.TryGetStringFieldQuickly("rc", ref _strRC);
            objXmlAccessory.TryGetBoolFieldQuickly("rcdeployable", ref _blnDeployable);
            objXmlAccessory.TryGetInt32FieldQuickly("rcgroup", ref _intRCGroup);
            objXmlAccessory.TryGetStringFieldQuickly("conceal", ref _strConceal);
            objXmlAccessory.TryGetInt32FieldQuickly("ammoslots", ref _intAmmoSlots);
            objXmlAccessory.TryGetStringFieldQuickly("ammoreplace", ref _strAmmoReplace);
            objXmlAccessory.TryGetInt32FieldQuickly("accuracy", ref _intAccuracy);
            objXmlAccessory.TryGetStringFieldQuickly("dicepool", ref _strDicePool);
            objXmlAccessory.TryGetStringFieldQuickly("damagetype", ref _strDamageType);
            objXmlAccessory.TryGetStringFieldQuickly("damage", ref _strDamage);
            objXmlAccessory.TryGetStringFieldQuickly("damagereplace", ref _strDamageReplace);
            objXmlAccessory.TryGetStringFieldQuickly("firemode", ref _strFireMode);
            objXmlAccessory.TryGetStringFieldQuickly("firemodereplace", ref _strFireModeReplace);
            objXmlAccessory.TryGetStringFieldQuickly("ap", ref _strAP);
            objXmlAccessory.TryGetStringFieldQuickly("apreplace", ref _strAPReplace);
            objXmlAccessory.TryGetStringFieldQuickly("addmode", ref _strAddMode);
            objXmlAccessory.TryGetInt32FieldQuickly("fullburst", ref _intFullBurst);
            objXmlAccessory.TryGetInt32FieldQuickly("suppressive", ref _intSuppressive);
            objXmlAccessory.TryGetInt32FieldQuickly("rangebonus", ref _intRangeBonus);
            objXmlAccessory.TryGetStringFieldQuickly("extra", ref _strExtra);
            objXmlAccessory.TryGetInt32FieldQuickly("ammobonus", ref _intAmmoBonus);
            objXmlAccessory.TryGetInt32FieldQuickly("accessorycostmultiplier", ref _intAccessoryCostMultiplier);

            // Add any Gear that comes with the Weapon Accessory.
            if (objXmlAccessory["gears"] != null && blnCreateChildren)
            {
                XmlDocument objXmlGearDocument = XmlManager.Load("gear.xml");
                foreach (XmlNode objXmlAccessoryGear in objXmlAccessory.SelectNodes("gears/usegear"))
                {
                    XmlNode objXmlAccessoryGearName = objXmlAccessoryGear["name"];
                    XmlAttributeCollection objXmlAccessoryGearNameAttributes = objXmlAccessoryGearName.Attributes;
                    int intGearRating = 0;
                    decimal decGearQty = 1;
                    string strChildForceSource = string.Empty;
                    string strChildForcePage = string.Empty;
                    string strChildForceValue = string.Empty;
                    bool blnStartCollapsed = objXmlAccessoryGearNameAttributes?["startcollapsed"]?.InnerText == "yes";
                    bool blnChildCreateChildren = objXmlAccessoryGearNameAttributes?["createchildren"]?.InnerText != "no";
                    bool blnAddChildImprovements = blnCreateImprovements;
                    if (objXmlAccessoryGearNameAttributes?["addimprovements"]?.InnerText == "no")
                        blnAddChildImprovements = false;
                    if (objXmlAccessoryGear["rating"] != null)
                        intGearRating = Convert.ToInt32(objXmlAccessoryGear["rating"].InnerText);
                    if (objXmlAccessoryGearNameAttributes?["qty"] != null)
                        decGearQty = Convert.ToDecimal(objXmlAccessoryGearNameAttributes["qty"].InnerText, GlobalOptions.InvariantCultureInfo);
                    if (objXmlAccessoryGearNameAttributes?["select"] != null)
                        strChildForceValue = objXmlAccessoryGearNameAttributes["select"].InnerText;
                    if (objXmlAccessoryGear["source"] != null)
                        strChildForceSource = objXmlAccessoryGear["source"].InnerText;
                    if (objXmlAccessoryGear["page"] != null)
                        strChildForcePage = objXmlAccessoryGear["page"].InnerText;

                    XmlNode objXmlGear = objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlAccessoryGearName.InnerText + "\" and category = \"" + objXmlAccessoryGear["category"].InnerText + "\"]");
                    Gear objGear = new Gear(_objCharacter);

                    TreeNode objGearNode = new TreeNode();
                    List<Weapon> lstWeapons = new List<Weapon>();
                    List<TreeNode> lstWeaponNodes = new List<TreeNode>();

                    if (!string.IsNullOrEmpty(objXmlGear["devicerating"]?.InnerText))
                    {
                        Commlink objCommlink = new Commlink(_objCharacter);
                        objCommlink.Create(objXmlGear, objGearNode, intGearRating, lstWeapons, lstWeaponNodes, strChildForceValue, false, false, blnAddChildImprovements, blnChildCreateChildren);
                        objGear = objCommlink;
                    }
                    else
                        objGear.Create(objXmlGear, objGearNode, intGearRating, lstWeapons, lstWeaponNodes, strChildForceValue, false, false, blnAddChildImprovements, blnChildCreateChildren);
                    objGear.Quantity = decGearQty;
                    objGear.Cost = "0";
                    objGear.MinRating = intGearRating;
                    objGear.MaxRating = intGearRating;
                    objGear.ParentID = InternalId;
                    if (!string.IsNullOrEmpty(strChildForceSource))
                        objGear.Source = strChildForceSource;
                    if (!string.IsNullOrEmpty(strChildForcePage))
                        objGear.Page = strChildForcePage;
                    _lstGear.Add(objGear);

                    // Change the Capacity of the child if necessary.
                    if (objXmlAccessoryGear["capacity"] != null)
                        objGear.Capacity = "[" + objXmlAccessoryGear["capacity"].InnerText + "]";
                    objGearNode.ContextMenuStrip = cmsAccessoryGear;
                    objGearNode.ForeColor = SystemColors.GrayText;
                    objNode.Nodes.Add(objGearNode);
                    if (!blnStartCollapsed)
                        objNode.Expand();
                }
            }

            if (GlobalOptions.Language != GlobalOptions.DefaultLanguage)
            {
                XmlNode objAccessoryNode = MyXmlNode;
                if (objAccessoryNode != null)
                {
                    _strAltName = objAccessoryNode["translate"]?.InnerText ?? string.Empty;
                    _strAltPage = objAccessoryNode["altpage"]?.InnerText ?? string.Empty;
                }
            }

            objNode.Text = DisplayName;
            objNode.Tag = _guiID.ToString();
        }

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlTextWriter objWriter)
        {
            objWriter.WriteStartElement("accessory");
            objWriter.WriteElementString("guid", _guiID.ToString());
            objWriter.WriteElementString("name", _strName);
            objWriter.WriteElementString("mount", _strMount);
            objWriter.WriteElementString("extramount", _strExtraMount);
            objWriter.WriteElementString("rc", _strRC);
            objWriter.WriteElementString("rating", _intRating.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("rcgroup", _intRCGroup.ToString(CultureInfo.InvariantCulture));
            objWriter.WriteElementString("rcdeployable", _blnDeployable.ToString());
            objWriter.WriteElementString("conceal", _strConceal);
            if (!string.IsNullOrEmpty(_strDicePool))
                objWriter.WriteElementString("dicepool", _strDicePool);
            objWriter.WriteElementString("avail", _strAvail);
            objWriter.WriteElementString("cost", _strCost);
            objWriter.WriteElementString("included", _blnIncludedInWeapon.ToString());
            objWriter.WriteElementString("installed", _blnInstalled.ToString());
            if (_nodAllowGear != null)
                objWriter.WriteRaw(_nodAllowGear.OuterXml);
            objWriter.WriteElementString("source", _strSource);
            objWriter.WriteElementString("page", _strPage);
            objWriter.WriteElementString("accuracy", _intAccuracy.ToString(CultureInfo.InvariantCulture));
            if (_lstGear.Count > 0)
            {
                objWriter.WriteStartElement("gears");
                foreach (Gear objGear in _lstGear)
                {
                    // Use the Gear's SubClass if applicable.
                    if (objGear.GetType() == typeof(Commlink))
                    {
                        Commlink objCommlink = new Commlink(_objCharacter);
                        objCommlink = (Commlink)objGear;
                        objCommlink.Save(objWriter);
                    }
                    else
                    {
                        objGear.Save(objWriter);
                    }
                }
                objWriter.WriteEndElement();
            }
            objWriter.WriteElementString("ammoslots", _intAmmoSlots.ToString());
            objWriter.WriteElementString("damagetype", _strDamageType);
            objWriter.WriteElementString("damage", _strDamage);
            objWriter.WriteElementString("damagereplace", _strDamageReplace);
            objWriter.WriteElementString("firemode", _strFireMode);
            objWriter.WriteElementString("firemodereplace", _strFireModeReplace);
            objWriter.WriteElementString("ap", _strAP);
            objWriter.WriteElementString("apreplace", _strAPReplace);
            objWriter.WriteElementString("notes", _strNotes);
            objWriter.WriteElementString("discountedcost", DiscountCost.ToString());
            objWriter.WriteElementString("addmode", _strAddMode);
            objWriter.WriteElementString("fullburst", _intFullBurst.ToString());
            objWriter.WriteElementString("suppressive", _intSuppressive.ToString());
            objWriter.WriteElementString("rangebonus", _intRangeBonus.ToString());
            objWriter.WriteElementString("extra", _strExtra);
            objWriter.WriteElementString("ammobonus", _intAmmoBonus.ToString());
            objWriter.WriteEndElement();
            _objCharacter.SourceProcess(_strSource);
        }

        /// <summary>
        /// Load the CharacterAttribute from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        /// <param name="blnCopy">Whether another node is being copied.</param>
        public void Load(XmlNode objNode, bool blnCopy = false)
        {
            if (blnCopy)
            {
                _guiID = Guid.NewGuid();
            }
            else
            {
                _guiID = Guid.Parse(objNode["guid"].InnerText);
            }
            if (objNode.TryGetStringFieldQuickly("name", ref _strName))
                _objCachedMyXmlNode = null;
            objNode.TryGetStringFieldQuickly("mount", ref _strMount);
            objNode.TryGetStringFieldQuickly("extramount", ref _strExtraMount);
            objNode.TryGetStringFieldQuickly("rc", ref _strRC);
            objNode.TryGetInt32FieldQuickly("rating", ref _intRating);
            objNode.TryGetInt32FieldQuickly("rcgroup", ref _intRCGroup);
            objNode.TryGetInt32FieldQuickly("accuracy", ref _intAccuracy);
            objNode.TryGetInt32FieldQuickly("rating", ref _intRating);
            objNode.TryGetStringFieldQuickly("conceal", ref _strConceal);
            objNode.TryGetBoolFieldQuickly("rcdeployable", ref _blnDeployable);
            objNode.TryGetStringFieldQuickly("avail", ref _strAvail);
            objNode.TryGetStringFieldQuickly("cost", ref _strCost);
            objNode.TryGetBoolFieldQuickly("included", ref _blnIncludedInWeapon);
            objNode.TryGetBoolFieldQuickly("installed", ref _blnInstalled);
            _nodAllowGear = objNode["allowgear"];
            objNode.TryGetStringFieldQuickly("source", ref _strSource);

            objNode.TryGetStringFieldQuickly("page", ref _strPage);
            objNode.TryGetStringFieldQuickly("dicepool", ref _strDicePool);

            objNode.TryGetInt32FieldQuickly("ammoslots", ref _intAmmoSlots);

            if (objNode.InnerXml.Contains("<gears>"))
            {
                XmlNodeList nodChildren = objNode.SelectNodes("gears/gear");
                foreach (XmlNode nodChild in nodChildren)
                {
                    if (nodChild["iscommlink"]?.InnerText == System.Boolean.TrueString || (nodChild["category"].InnerText == "Commlinks" ||
                        nodChild["category"].InnerText == "Commlink Accessories" || nodChild["category"].InnerText == "Cyberdecks" || nodChild["category"].InnerText == "Rigger Command Consoles"))
                    {
                        Gear objCommlink = new Commlink(_objCharacter);
                        objCommlink.Load(nodChild, blnCopy);
                        _lstGear.Add(objCommlink);
                    }
                    else
                    {
                        Gear objGear = new Gear(_objCharacter);
                        objGear.Load(nodChild, blnCopy);
                        _lstGear.Add(objGear);
                    }
                }
            }
            objNode.TryGetStringFieldQuickly("notes", ref _strNotes);
            objNode.TryGetBoolFieldQuickly("discountedcost", ref _blnDiscountCost);

            if (GlobalOptions.Language != GlobalOptions.DefaultLanguage)
            {
                XmlNode objAccessoryNode = MyXmlNode;
                if (objAccessoryNode != null)
                {
                    objAccessoryNode.TryGetStringFieldQuickly("translate", ref _strAltName);
                    objAccessoryNode.TryGetStringFieldQuickly("altpage", ref _strAltPage);
                }
            }
            objNode.TryGetStringFieldQuickly("damage", ref _strDamage);
            objNode.TryGetStringFieldQuickly("damagetype", ref _strDamageType);
            objNode.TryGetStringFieldQuickly("damagereplace", ref _strDamageReplace);
            objNode.TryGetStringFieldQuickly("firemode", ref _strFireMode);
            objNode.TryGetStringFieldQuickly("firemodereplace", ref _strFireModeReplace);
            objNode.TryGetStringFieldQuickly("ap", ref _strAP);
            objNode.TryGetStringFieldQuickly("apreplace", ref _strAPReplace);
            objNode.TryGetInt32FieldQuickly("accessorycostmultiplier", ref _intAccessoryCostMultiplier);
            objNode.TryGetStringFieldQuickly("addmode", ref _strAddMode);
            objNode.TryGetInt32FieldQuickly("fullburst", ref _intFullBurst);
            objNode.TryGetInt32FieldQuickly("suppressive", ref _intSuppressive);
            objNode.TryGetInt32FieldQuickly("rangebonus", ref _intRangeBonus);
            objNode.TryGetStringFieldQuickly("extra", ref _strExtra);
            objNode.TryGetInt32FieldQuickly("ammobonus", ref _intAmmoBonus);
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Print(XmlTextWriter objWriter, CultureInfo objCulture)
        {
            objWriter.WriteStartElement("accessory");
            objWriter.WriteElementString("name", DisplayName);
            objWriter.WriteElementString("mount", _strMount);
            objWriter.WriteElementString("extramount", _strExtraMount);
            objWriter.WriteElementString("rc", _strRC);
            objWriter.WriteElementString("conceal", _strConceal);
            objWriter.WriteElementString("avail", TotalAvail);
            objWriter.WriteElementString("cost", TotalCost.ToString(_objCharacter.Options.NuyenFormat, objCulture));
            objWriter.WriteElementString("owncost", OwnCost.ToString(_objCharacter.Options.NuyenFormat, objCulture));
            objWriter.WriteElementString("included", _blnIncludedInWeapon.ToString());
            objWriter.WriteElementString("source", _objCharacter.Options.LanguageBookShort(_strSource));
            objWriter.WriteElementString("page", Page);
            objWriter.WriteElementString("accuracy", _intAccuracy.ToString(objCulture));
            if (_lstGear.Count > 0)
            {
                objWriter.WriteStartElement("gears");
                foreach (Gear objGear in _lstGear)
                {
                    // Use the Gear's SubClass if applicable.
                    Commlink objCommlink = objGear as Commlink;
                    if (objCommlink != null)
                    {
                        objCommlink.Print(objWriter, objCulture);
                    }
                    else
                    {
                        objGear.Print(objWriter, objCulture);
                    }
                }
                objWriter.WriteEndElement();
            }
            if (_objCharacter.Options.PrintNotes)
                objWriter.WriteElementString("notes", _strNotes);
            objWriter.WriteEndElement();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Internal identifier which will be used to identify this Weapon.
        /// </summary>
        public string InternalId
        {
            get
            {
                return _guiID.ToString();
            }
        }

        /// <summary>
        /// Name.
        /// </summary>
        public string Name
        {
            get
            {
                return _strName;
            }
            set
            {
                if (_strName != value)
                {
                    _objCachedMyXmlNode = null;
                    _strName = value;
                }
            }
        }
        /// <summary>
        /// The accessory adds to the weapon's ammunition slots.
        /// </summary>
        public int AmmoSlots
        {
            get
            {
                return _intAmmoSlots;
            }
            set
            {
                _intAmmoSlots = value;
            }
        }
        /// <summary>
        /// The accessory adds to the weapon's damage value.
        /// </summary>
        public string Damage
        {
            get
            {
                return _strDamage;
            }
            set
            {
                _strDamage = value;
            }
        }
        /// <summary>
        /// The Accessory replaces the weapon's damage value.
        /// </summary>
        public string DamageReplacement
        {
            get
            {
                return _strDamageReplace;
            }
            set
            {
                _strDamageReplace = value;
            }
        }

        /// <summary>
        /// The Accessory changes the Damage Type.
        /// </summary>
        public string DamageType
        {
            get
            {
                return _strDamageType;
            }
            set
            {
                _strDamageType = value;
            }
        }

        /// <summary>
        /// The accessory adds to the weapon's Armor Penetration.
        /// </summary>
        public string AP
        {
            get
            {
                return _strAP;
            }
            set
            {
                _strAP = value;
            }
        }

        /// <summary>
        /// Whether the Accessory only grants a Recoil Bonus while deployed.
        /// </summary>
        public bool RCDeployable
        {
            get
            {
                return _blnDeployable;
            }
        }

        /// <summary>
        /// Accuracy.
        /// </summary>
        public int Accuracy
        {
            get
            {
                if (_blnInstalled)
                    return _intAccuracy;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Concealability.
        /// </summary>
        public string APReplacement
        {
            get
            {
                return _strAPReplace;
            }
            set
            {
                _strAPReplace = value;
            }
        }

        /// <summary>
        /// The accessory adds a Fire Mode to the weapon.
        /// </summary>
        public string FireMode
        {
            get
            {
                return _strFireMode;
            }
            set
            {
                _strFireMode = value;
            }
        }

        /// <summary>
        /// The accessory replaces the weapon's Fire Modes.
        /// </summary>
        public string FireModeReplacement
        {
            get
            {
                return _strFireModeReplace;
            }
            set
            {
                _strFireModeReplace = value;
            }
        }

        /// <summary>
        /// The name of the object as it should appear on printouts (translated name only).
        /// </summary>
        public string DisplayNameShort
        {
            get
            {
                if (!string.IsNullOrEmpty(_strAltName))
                    return _strAltName;

                return _strName;
            }
        }

        /// <summary>
        /// The name of the object as it should be displayed in lists. Name (Extra).
        /// </summary>
        public string DisplayName
        {
            get
            {
                return DisplayNameShort;
            }
        }

        /// <summary>
        /// Mount Used.
        /// </summary>
        public string Mount
        {
            get
            {
                return _strMount;
            }
            set
            {
                _strMount = value;
            }
        }
        
        /// <summary>
        /// Additional mount slot used (if any).
        /// </summary>
        public string ExtraMount
        {
            get
            {
                return _strExtraMount;
            }
            set
            {
                _strExtraMount = value;
            }
        }

        /// <summary>
        /// Recoil.
        /// </summary>
        public string RC
        {
            get
            {
                return _strRC;
            }
            set
            {
                _strRC = value;
            }
        }

        /// <summary>
        /// Recoil Group.
        /// </summary>
        public int RCGroup
        {
            get
            {
                return _intRCGroup;
            }
        }

        /// <summary>
        /// Concealability.
        /// </summary>
        public int Concealability
        {
            get
            {
                int intReturn = 0;

                if (_strConceal.Contains("Rating"))
                {
                    // If the cost is determined by the Rating, evaluate the expression.
                    string strConceal = string.Empty;
                    string strCostExpression = _strConceal;

                    strConceal = strCostExpression.Replace("Rating", _intRating.ToString());
                    try
                    {
                        intReturn = Convert.ToInt32(Math.Ceiling((double)CommonFunctions.EvaluateInvariantXPath(strConceal)));
                    }
                    catch (XPathException) { }
                    catch (OverflowException) { }
                    catch (InvalidCastException) { }
                }
                else if (!string.IsNullOrEmpty(_strConceal))
                {
                    int.TryParse(_strConceal, out intReturn);
                }
                return intReturn;
            }
            set
            {
                _strConceal = value.ToString();
            }
        }

        /// <summary>
        /// Concealability.
        /// </summary>
        public int Rating
        {
            get
            {
                return _intRating;
            }
            set
            {
                _intRating = value;
            }
        }

        /// <summary>
        /// Avail.
        /// </summary>
        public string Avail
        {
            get
            {
                return _strAvail;
            }
            set
            {
                _strAvail = value;
            }
        }

        /// <summary>
        /// Cost.
        /// </summary>
        public string Cost
        {
            get
            {
                // The Accessory has a cost of 0 if it is included in the base weapon configureation.
                if (_blnIncludedInWeapon)
                    return "0";
                else
                    return _strCost;
            }
            set
            {
                _strCost = value;
            }
        }

        /// <summary>
        /// Sourcebook.
        /// </summary>
        public string Source
        {
            get
            {
                return _strSource;
            }
            set
            {
                _strSource = value;
            }
        }

        /// <summary>
        /// Sourcebook Page Number.
        /// </summary>
        public string Page
        {
            get
            {
                if (!string.IsNullOrEmpty(_strAltPage))
                    return _strAltPage;

                return _strPage;
            }
            set
            {
                _strPage = value;
            }
        }

        /// <summary>
        /// Whether or not this Accessory is part of the base weapon configuration.
        /// </summary>
        public bool IncludedInWeapon
        {
            get
            {
                return _blnIncludedInWeapon;
            }
            set
            {
                _blnIncludedInWeapon = value;
            }
        }

        /// <summary>
        /// Whether or not this Accessory is installed and contributing towards the Weapon's stats.
        /// </summary>
        public bool Installed
        {
            get
            {
                return _blnInstalled;
            }
            set
            {
                _blnInstalled = value;
            }
        }

        /// <summary>
        /// Notes.
        /// </summary>
        public string Notes
        {
            get
            {
                return _strNotes;
            }
            set
            {
                _strNotes = value;
            }
        }

        /// <summary>
        /// Total Availability.
        /// </summary>
        public string TotalAvail
        {
            get
            {
                // If the Avail contains "+", return the base string and don't try to calculate anything since we're looking at a child component.
                
                string strCalculated = string.Empty;
                string strReturn = string.Empty;

                if (_strAvail.Contains("Rating"))
                {
                    // If the availability is determined by the Rating, evaluate the expression.
                    string strAvail = string.Empty;
                    string strAvailExpr = _strAvail;

                    if (strAvailExpr.Substring(strAvailExpr.Length - 1, 1) == "F" || strAvailExpr.Substring(strAvailExpr.Length - 1, 1) == "R")
                    {
                        strAvail = strAvailExpr.Substring(strAvailExpr.Length - 1, 1);
                        // Remove the trailing character if it is "F" or "R".
                        strAvailExpr = strAvailExpr.Substring(0, strAvailExpr.Length - 1);
                    }
                    strCalculated = Convert.ToInt32(CommonFunctions.EvaluateInvariantXPath(strAvailExpr.Replace("Rating", _intRating.ToString()))).ToString() + strAvail;
                }
                else
                {
                    // Just a straight cost, so return the value.
                    if (_strAvail.Contains("F") || _strAvail.Contains("R"))
                    {
                        strCalculated = Convert.ToInt32(_strAvail.Substring(0, _strAvail.Length - 1)).ToString() + _strAvail.Substring(_strAvail.Length - 1, 1);
                    }
                    else
                        strCalculated = Convert.ToInt32(_strAvail).ToString();
                }

                int intAvail = 0;
                string strAvailText = string.Empty;
                if (strCalculated.Contains("F") || strCalculated.Contains("R"))
                {
                    strAvailText = strCalculated.Substring(strCalculated.Length - 1);
                    intAvail = Convert.ToInt32(strCalculated.Substring(0, strCalculated.Length - 1));
                }
                else
                    intAvail = Convert.ToInt32(strCalculated);

                // Translate the Avail string.
                if (strAvailText == "R")
                    strAvailText = LanguageManager.GetString("String_AvailRestricted");
                else if (strAvailText == "F")
                    strAvailText = LanguageManager.GetString("String_AvailForbidden");
                strReturn = intAvail.ToString() + strAvailText;

                return strReturn;
            }
        }

        /// <summary>
        /// AllowGear node from the XML file.
        /// </summary>
        public XmlNode AllowGear
        {
            get
            {
                return _nodAllowGear;
            }
            set
            {
                _nodAllowGear = value;
            }
        }

        /// <summary>
        /// A List of the Gear attached to the Cyberware.
        /// </summary>
        public List<Gear> Gear
        {
            get
            {
                return _lstGear;
            }
        }

        /// <summary>
        /// Whether or not the Armor's cost should be discounted by 10% through the Black Market Pipeline Quality.
        /// </summary>
        public bool DiscountCost
        {
            get
            {
                return _blnDiscountCost;
            }
            set
            {
                _blnDiscountCost = value;
            }
        }

        /// <summary>
        /// Parent Weapon.
        /// </summary>
        public Weapon Parent
        {
            get
            {
                return _objParent;
            }
            set
            {
                _objParent = value;
            }
        }

        /// <summary>
        /// Total cost of the Weapon Accessory.
        /// </summary>
        public decimal TotalCost
        {
            get
            {
                decimal decReturn = OwnCost;

                // Add in the cost of any Gear the Weapon Accessory has attached to it.
                foreach (Gear objGear in _lstGear)
                    decReturn += objGear.TotalCost;

                return decReturn;
            }
        }

        /// <summary>
        /// The cost of just the Weapon Accessory itself.
        /// </summary>
        public decimal OwnCost
        {
            get
            {
                decimal decReturn = 0;
                string strCost = string.Empty;
                string strCostExpression = _strCost;

                strCost = strCostExpression.Replace("Weapon Cost", _objParent.Cost.ToString());
                strCost = strCost.Replace("Rating", _intRating.ToString());
                decReturn = Convert.ToDecimal(CommonFunctions.EvaluateInvariantXPath(strCost).ToString(), GlobalOptions.InvariantCultureInfo) * _objParent.CostMultiplier;

                if (DiscountCost)
                    decReturn *= 0.9m;

                return decReturn;
            }
        }

        /// <summary>
        /// Dice Pool modifier.
        /// </summary>
        public int DicePool
        {
            get
            {
                if (!string.IsNullOrEmpty(_strDicePool))
                    return Convert.ToInt32(_strDicePool);

                return 0;
            }
        }

        private string DicePoolString
        {
            get
            {
                return _strDicePool;
            }
        }

        /// <summary>
        /// Adjust the Weapon's Ammo amount by the specified percent.
        /// </summary>
        public int AmmoBonus
        {
            get
            {
                return _intAmmoBonus;
            }
            set
            {
                _intAmmoBonus = value;
            }
        }

        /// <summary>
        /// Replace the Weapon's Ammo value with the Weapon Mod's value.
        /// </summary>
        public string AmmoReplace
        {
            get
            {
                return _strAmmoReplace;
            }
            set
            {
                _strAmmoReplace = value;
            }
        }

        /// <summary>
        /// Multiply the cost of other installed Accessories.
        /// </summary>
        public int AccessoryCostMultiplier
        {
            get
            {
                return _intAccessoryCostMultiplier;
            }
            set
            {
                _intAccessoryCostMultiplier = value;
            }
        }

        /// <summary>
        /// Additional Weapon Firing Mode.
        /// </summary>
        public string AddMode
        {
            get
            {
                return _strAddMode;
            }
            set
            {
                _strAddMode = value;
            }
        }

        /// <summary>
        /// Number of rounds consumed by Full Burst.
        /// </summary>
        public int FullBurst
        {
            get
            {
                return _intFullBurst;
            }
        }

        /// <summary>
        /// Number of rounds consumed by Suppressive Fire.
        /// </summary>
        public int Suppressive
        {
            get
            {
                return _intSuppressive;
            }
        }

        /// <summary>
        /// Range bonus granted by the Accessory.
        /// </summary>
        public int RangeBonus
        {
            get
            {
                return _intRangeBonus;
            }
        }

        /// <summary>
        /// Value that was selected during an ImprovementManager dialogue.
        /// </summary>
        public string Extra
        {
            get
            {
                return _strExtra;
            }
            set
            {
                _strExtra = value;
            }
        }
        
        /// <summary>
        /// Whether the Accessory is affected by Black Market Discounts.
        /// </summary>
        public bool BlackMarketDiscount
        {
            get
            {
                return _blnBlackMarketDiscount;
            }
            set
            {
                _blnBlackMarketDiscount = value;
            }
        }

        private XmlNode _objCachedMyXmlNode = null;
        public XmlNode MyXmlNode
        {
            get
            {
                if (_objCachedMyXmlNode == null || GlobalOptions.LiveCustomData)
                    _objCachedMyXmlNode = XmlManager.Load("weapons.xml")?.SelectSingleNode("/chummer/accessories/accessory[name = \"" + Name + "\"]");
                return _objCachedMyXmlNode;
            }
        }
        #endregion
    }
}
