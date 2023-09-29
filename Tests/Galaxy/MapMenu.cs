using GLOFC.GL4.Controls;
using GLOFC;
using System;
using System.Collections.Generic;
using System.Drawing;
using GLOFC.Utils;
using static GLOFC.GL4.Controls.GLBaseControl;
using OpenTK;
using System.Web.UI.DataVisualization.Charting;
using System.Linq;
using System.Windows.Forms;
using QuickJSON;

namespace TestOpenTk
{
    public class MapMenu
    {
        private Map map;
        private ImageCache images;
        private GLLabel status;
        private const int iconsize = 32;
        private bool orderedclosemainmenu = false;  // import

        public const string EntryTextName = "MSEntryText";

        public MapMenu(Map g, ImageCache i)
        {
            map = g;
            images = i;

            GLBaseControl.Themer = Theme;

            // names of MS* are on screen items hidden during main menu presentation

            GLImage menuimage = new GLImage("MSMainMenu", new Rectangle(10, 10, iconsize, iconsize), Properties.Resources.hamburgermenu);
            menuimage.ToolTipText = "Open configuration menu";
            map.displaycontrol.Add(menuimage);
            menuimage.MouseClick = (o, e1) => {
                //ShowMenu(); 
                //ShowVector3Menu(new Vector3(1, 2, 3), "fred", (v)=> { System.Diagnostics.Debug.WriteLine($"Vector {v}"); });
                ShowImagesMenu(images.GetImageList());
            };

            if (true)
            {
                GLImage tpback = new GLImage("MSTPBack", new Rectangle(50, 10, iconsize, iconsize), Properties.Resources.GoBackward);
                tpback.ToolTipText = "Go back one system";
                map.displaycontrol.Add(tpback);
                tpback.MouseClick = (o, e1) => { g.GoToTravelSystem(-1); };

                GLImage tphome = new GLImage("MSTPHome", new Rectangle(90, 10, iconsize, iconsize), Properties.Resources.GoToHomeSystem);
                tphome.ToolTipText = "Go to current home system";
                map.displaycontrol.Add(tphome);
                tphome.MouseClick = (o, e1) => { g.GoToTravelSystem(0); };

                GLImage tpforward = new GLImage("MSTPForward", new Rectangle(130, 10, iconsize, iconsize), Properties.Resources.GoForward);
                tpforward.ToolTipText = "Go forward one system";
                map.displaycontrol.Add(tpforward);
                tpforward.MouseClick = (o, e1) => { g.GoToTravelSystem(1); };

                GLTextBoxAutoComplete tptextbox = new GLTextBoxAutoComplete(EntryTextName, new Rectangle(170, 10, 300, iconsize), "");
                tptextbox.TextAlign = ContentAlignment.MiddleLeft;
                map.displaycontrol.Add(tptextbox);
            }


            GLToolTip maintooltip = new GLToolTip("MTT", Color.FromArgb(180, 50, 50, 50));
            maintooltip.ForeColor = Color.Orange;
            map.displaycontrol.Add(maintooltip);

            if (true)
            {
                status = new GLLabel("Status", new Rectangle(10, 500, 600, 24), "x");
                status.Dock = DockingType.BottomLeft;
                status.ForeColor = Color.Orange;
                status.BackColor = Color.FromArgb(50, 50, 50, 50);
                map.displaycontrol.Add(status);
            }


            // detect mouse press with menu open and close it
            map.displaycontrol.GlobalMouseDown += (ctrl, e) =>
            {
                // if map open, and no ctrl hit or ctrl is not a child of galmenu

                GLForm mapform = map.displaycontrol["Galmenu"] as GLForm;

                if (mapform != null && ctrl == map.displaycontrol && map.displaycontrol.ModalFormsActive == false && mapform.FormShown && !orderedclosemainmenu)
                {
                    System.Diagnostics.Debug.WriteLine($"Ordered close");       // import
                    ((GLForm)mapform).Close();
                }
            };
        }

        // on menu button..

        public void ShowMenu()
        {
            map.displaycontrol.ApplyToControlOfName("InfoBoxForm*", (c) => { ((GLForm)c).Close(); });      // close any info box forms
            map.displaycontrol.ApplyToControlOfName("MS*", (c) => { c.Visible = false; });      // hide the visiblity of the on screen controls

            int leftmargin = 4;
            int vpos = 10;
            int ypad = 10;
            int hpad = 8;

            orderedclosemainmenu = false;       // reset

            GLForm pform = new GLForm("Galmenu", "Configure Map", new Rectangle(10, 10, 600, 600), true);
            pform.FormClosed = (frm) => { map.displaycontrol.ApplyToControlOfName("MS*", (c) => { c.Visible = true; }); };

            // provide opening animation
            pform.ScaleWindow = new SizeF(0.0f, 0.0f);
            pform.Animators.Add(new GLControlAnimateScale(10, 400, true, new SizeF(1, 1)));

            // and closing animation
            pform.FormClosing += (f,e) => { 
                e.Handled = true;       // stop close
                orderedclosemainmenu = true;
                var ani = new GLControlAnimateScale(10, 400, true, new SizeF(0, 0));       // add a close animation
                ani.FinishAction += (a, c, t) => { pform.ForceClose();  };   // when its complete, force close
                pform.Animators.Add(ani); 
            };

            {   // top buttons
                GLPanel p3d2d = new GLPanel("3d2d", new Rectangle(leftmargin, vpos, 80, iconsize), Color.Transparent);

                GLCheckBox but3d = new GLCheckBox("3d", new Rectangle(0, 0, iconsize, iconsize), Properties.Resources._3d, null);
                but3d.Checked = map.gl3dcontroller.MatrixCalc.InPerspectiveMode;
                but3d.ToolTipText = "3D View";
                but3d.GroupRadioButton = true;
                but3d.MouseClick += (e1, e2) => { map.gl3dcontroller.ChangePerspectiveMode(true); };
                p3d2d.Add(but3d);

                GLCheckBox but2d = new GLCheckBox("2d", new Rectangle(50, 0, iconsize, iconsize), Properties.Resources._2d, null);
                but2d.Checked = !map.gl3dcontroller.MatrixCalc.InPerspectiveMode;
                but2d.ToolTipText = "2D View";
                but2d.GroupRadioButton = true;
                but2d.MouseClick += (e1, e2) => { map.gl3dcontroller.ChangePerspectiveMode(false); };
                p3d2d.Add(but2d);

                pform.Add(p3d2d);

                GLCheckBox butelite = new GLCheckBox("Elite", new Rectangle(100, vpos, iconsize, iconsize), Properties.Resources.EliteMovement, null);
                butelite.ToolTipText = "Select elite movement (on Y plain)";
                butelite.Checked = map.gl3dcontroller.YHoldMovement;
                butelite.CheckChanged += (e1) => { map.gl3dcontroller.YHoldMovement = butelite.Checked; };
                pform.Add(butelite);

                GLCheckBox butgal = new GLCheckBox("Galaxy", new Rectangle(150, vpos, iconsize, iconsize), Properties.Resources.ShowGalaxy, null);
                butgal.ToolTipText = "Show galaxy image";
                butgal.Checked = map.GalaxyDisplay;
                butgal.CheckChanged += (e1) => { map.GalaxyDisplay = butgal.Checked; };
                pform.Add(butgal);

                GLCheckBox butsd = new GLCheckBox("StarDots", new Rectangle(200, vpos, iconsize, iconsize), Properties.Resources.StarDots, null);
                butsd.ToolTipText = "Show star field";
                butsd.Checked = map.StarDotsDisplay;
                butsd.CheckChanged += (e1) => { map.StarDotsDisplay = butsd.Checked; };
                pform.Add(butsd);

                vpos += butgal.Height + ypad;
            }


            {
                GLGroupBox tpgb = new GLGroupBox("GalaxyStarsGB", "Galaxy Stars", new Rectangle(leftmargin, vpos, pform.ClientWidth - leftmargin * 2, iconsize * 2));
                pform.Add(tpgb);

                int hpos = leftmargin;

                GLCheckBox butgalstars = new GLCheckBox("GalaxyStars", new Rectangle(hpos, 0, iconsize, iconsize), Properties.Resources.StarDots, null);
                butgalstars.ToolTipText = "Show stars when zoomed in";
                butgalstars.Checked = (map.GalaxyStars & 1) != 0;
                butgalstars.CheckChanged += (e1) => { map.GalaxyStars ^= 1; };
                tpgb.Add(butgalstars);
                hpos += butgalstars.Width + hpad;

                GLCheckBox butgalstarstext = new GLCheckBox("GalaxyStarsText", new Rectangle(hpos, 0, iconsize, iconsize), Properties.Resources.StarDots, null);
                butgalstarstext.ToolTipText = "Show names of stars when zoomed in";
                butgalstarstext.Checked = (map.GalaxyStars & 2) != 0;
                butgalstarstext.CheckChanged += (e1) => { map.GalaxyStars ^= 2; };
                tpgb.Add(butgalstarstext);
                hpos += butgalstarstext.Width + hpad;

                vpos += tpgb.Height + ypad;
            }

            {
                GLGroupBox tpgb = new GLGroupBox("TravelPathGB", "Travel Path", new Rectangle(leftmargin, vpos, pform.ClientWidth - leftmargin * 2, iconsize *2));
                pform.Add(tpgb);

                GLCheckBox buttp = new GLCheckBox("TravelPath", new Rectangle(leftmargin, 0, iconsize, iconsize), Properties.Resources.StarDots, null);
                buttp.ToolTipText = "Show travel path";
                buttp.Checked = map.TravelPathDisplay;
                buttp.CheckChanged += (e1) => { map.TravelPathDisplay = buttp.Checked; };
                tpgb.Add(buttp);

                GLDateTimePicker dtps = new GLDateTimePicker("TPStart", new Rectangle(50, 0, 250, 30), DateTime.Now);
                dtps.Font = new Font("Ms Sans Serif", 8.25f);
                dtps.ShowCheckBox = dtps.ShowCalendar = true;
                dtps.Value = map.TravelPathStartDate;
                dtps.Checked = map.TravelPathStartDateEnable;
                dtps.ValueChanged += (e1) => { map.TravelPathStartDate = dtps.Value; map.TravelPathRefresh(); };
                dtps.CheckChanged += (e1) => { map.TravelPathStartDateEnable = dtps.Checked; map.TravelPathRefresh(); };
                dtps.ShowUpDown = true;
                tpgb.Add(dtps);

                GLDateTimePicker dtpe = new GLDateTimePicker("TPEnd", new Rectangle(320, 0, 250, 30), DateTime.Now);
                dtpe.Font = new Font("Ms Sans Serif", 8.25f);
                dtpe.ShowCheckBox = dtps.ShowCalendar = true;
                dtpe.Value = map.TravelPathEndDate;
                dtpe.Checked = map.TravelPathEndDateEnable;
                dtpe.ValueChanged += (e1) => { map.TravelPathEndDate = dtpe.Value; map.TravelPathRefresh(); };
                dtpe.CheckChanged += (e1) => { map.TravelPathEndDateEnable = dtpe.Checked; map.TravelPathRefresh(); };
                dtpe.ShowUpDown = true;
                tpgb.Add(dtpe);


                vpos += tpgb.Height + ypad;
            }

            { // Galaxy objects
                GLGroupBox galgb = new GLGroupBox("GalGB", "Galaxy Objects", new Rectangle(leftmargin, vpos, pform.ClientWidth - leftmargin * 2, 50));
                galgb.ClientHeight = (iconsize + 4) * 2;
                pform.Add(galgb);

                GLFlowLayoutPanel galfp = new GLFlowLayoutPanel("GALFP", DockingType.Fill, 0);
                galfp.FlowPadding = new PaddingType(2, 2, 2, 2);
                galgb.Add(galfp);

                for (int i = EliteDangerousCore.EDSM.GalMapType.VisibleTypes.Length - 1; i >= 0; i--)
                {
                    var gt = EliteDangerousCore.EDSM.GalMapType.VisibleTypes[i];
                    bool en = map.GetGalObjectTypeEnable(gt.TypeName);
                    GLCheckBox butg = new GLCheckBox("GMSEL"+i, new Rectangle(0, 0, iconsize, iconsize), GalMapObjects.GalMapTypeIcons[gt.VisibleType.Value], null);
                    butg.ToolTipText = "Enable/Disable " + gt.Description;
                    butg.Checked = en;
                    butg.CheckChanged += (e1) =>
                    {
                        map.SetGalObjectTypeEnable(gt.TypeName, butg.Checked);
                    };
                    galfp.Add(butg);
                }

                GLCheckBox butgonoff = new GLCheckBox("GMONOFF", new Rectangle(0, 0, iconsize, iconsize), Properties.Resources.dotted, null);
                butgonoff.ToolTipText = "Enable/Disable Display";
                butgonoff.Checked = map.GalObjectDisplay;
                butgonoff.CheckChanged += (e1) => { map.GalObjectDisplay = !map.GalObjectDisplay; };
                galfp.Add(butgonoff);

                vpos += galgb.Height + ypad;
            }

            { // EDSM regions
                GLGroupBox edsmregionsgb = new GLGroupBox("EDSMR", "EDSM Regions", new Rectangle(leftmargin, vpos, pform.ClientWidth - leftmargin * 2, 50));
                edsmregionsgb.ClientHeight = iconsize + 8;
                pform.Add(edsmregionsgb);
                vpos += edsmregionsgb.Height + ypad;

                GLCheckBox butedre = new GLCheckBox("EDSMRE", new Rectangle(leftmargin, 0, iconsize, iconsize), Properties.Resources.ShowGalaxy, null);
                butedre.ToolTipText = "Enable EDSM Regions";
                butedre.Checked = map.EDSMRegionsEnable;
                butedre.UserCanOnlyCheck = true;
                edsmregionsgb.Add(butedre);

                GLCheckBox buted2 = new GLCheckBox("EDSMR2", new Rectangle(50, 0, iconsize, iconsize), Properties.Resources.ShowGalaxy, null);
                buted2.Checked = map.EDSMRegionsOutlineEnable;
                buted2.Enabled = map.EDSMRegionsEnable;
                buted2.ToolTipText = "Enable Region Outlines";
                buted2.CheckChanged += (e1) => { map.EDSMRegionsOutlineEnable = !map.EDSMRegionsOutlineEnable; };
                edsmregionsgb.Add(buted2);

                GLCheckBox buted3 = new GLCheckBox("EDSMR3", new Rectangle(100, 0, iconsize, iconsize), Properties.Resources.ShowGalaxy, null);
                buted3.Checked = map.EDSMRegionsShadingEnable;
                buted3.Enabled = map.EDSMRegionsEnable;
                buted3.ToolTipText = "Enable Region Shading";
                buted3.CheckChanged += (e1) => { map.EDSMRegionsShadingEnable = !map.EDSMRegionsShadingEnable; };
                edsmregionsgb.Add(buted3);

                GLCheckBox buted4 = new GLCheckBox("EDSMR4", new Rectangle(150, 0, iconsize, iconsize), Properties.Resources.ShowGalaxy, null);
                buted4.Checked = map.EDSMRegionsTextEnable;
                buted4.Enabled = map.EDSMRegionsEnable;
                buted4.ToolTipText = "Enable Region Naming";
                buted4.CheckChanged += (e1) => { map.EDSMRegionsTextEnable = !map.EDSMRegionsTextEnable; };
                edsmregionsgb.Add(buted4);

                // elite regions

                GLGroupBox eliteregionsgb = new GLGroupBox("ELITER", "Elite Regions", new Rectangle(leftmargin, vpos, pform.ClientWidth - leftmargin * 2, 50));
                eliteregionsgb.ClientHeight = iconsize + 8;
                pform.Add(eliteregionsgb);

                vpos += eliteregionsgb.Height + ypad;

                GLCheckBox butelre = new GLCheckBox("ELITERE", new Rectangle(leftmargin, 0, iconsize, iconsize), Properties.Resources.ShowGalaxy, null);
                butelre.ToolTipText = "Enable Elite Regions";
                butelre.Checked = map.EliteRegionsEnable;
                butelre.UserCanOnlyCheck = true;
                eliteregionsgb.Add(butelre);

                GLCheckBox butel2 = new GLCheckBox("ELITER2", new Rectangle(50, 0, iconsize, iconsize), Properties.Resources.ShowGalaxy, null);
                butel2.Checked = map.EliteRegionsOutlineEnable;
                butel2.Enabled = map.EliteRegionsEnable;
                butel2.ToolTipText = "Enable Region Outlines";
                butel2.CheckChanged += (e1) => { map.EliteRegionsOutlineEnable = !map.EliteRegionsOutlineEnable; };
                eliteregionsgb.Add(butel2);

                GLCheckBox butel3 = new GLCheckBox("ELITER3", new Rectangle(100, 0, iconsize, iconsize), Properties.Resources.ShowGalaxy, null);
                butel3.Checked = map.EliteRegionsShadingEnable;
                butel3.Enabled = map.EliteRegionsEnable;
                butel3.ToolTipText = "Enable Region Shading";
                butel3.CheckChanged += (e1) => { map.EliteRegionsShadingEnable = !map.EliteRegionsShadingEnable; };
                eliteregionsgb.Add(butel3);

                GLCheckBox butel4 = new GLCheckBox("ELITER4", new Rectangle(150, 0, iconsize, iconsize), Properties.Resources.ShowGalaxy, null);
                butel4.Checked = map.EliteRegionsTextEnable;
                butel4.Enabled = map.EliteRegionsEnable;
                butel4.ToolTipText = "Enable Region Naming";
                butel4.CheckChanged += (e1) => { map.EliteRegionsTextEnable = !map.EliteRegionsTextEnable; };
                eliteregionsgb.Add(butel4);

                butedre.CheckChanged += (e) =>
                {
                    if (e.Name == "EDSMRE")
                    {
                        butelre.CheckedNoChangeEvent = !butedre.Checked;
                    }
                    else
                    {
                        butedre.CheckedNoChangeEvent = !butelre.Checked;
                    }

                    map.EDSMRegionsEnable = butedre.Checked;
                    map.EliteRegionsEnable = butelre.Checked;

                    buted2.Enabled = buted3.Enabled = buted4.Enabled = butedre.Checked;
                    butel2.Enabled = butel3.Enabled = butel4.Enabled = butelre.Checked;
                };

                butelre.CheckChanged += butedre.CheckChanged;
            }

            { // Images
                GLGroupBox imagesgb = new GLGroupBox("Images", "Overlay Images", new Rectangle(leftmargin, vpos, pform.ClientWidth - leftmargin * 2, 50));
                imagesgb.ClientHeight = iconsize + 8;
                pform.Add(imagesgb);

                GLCheckBox b1 = new GLCheckBox("ImagesEnable", new Rectangle(leftmargin, 0, iconsize, iconsize), Properties.Resources.ShowGalaxy, null);
                b1.ToolTipText = "Enable Images";
                b1.Checked = map.UserImagesEnable;
                b1.CheckOnClick = true;
                b1.CheckChanged += (e1) => { map.UserImagesEnable = b1.Checked; };
                imagesgb.Add(b1);

                GLButton b2 = new GLButton("ImagesConfigure", new Rectangle(50, 0, iconsize, iconsize), Properties.Resources.ShowGalaxy, true);
                b2.ToolTipText = "Configure Images";
                b2.Click += (e, s) => ShowImagesMenu(images.GetImageList());
                imagesgb.Add(b2);

                vpos += imagesgb.Height + ypad;
            }


            map.displaycontrol.Add(pform);
        }


        public void ShowImagesMenu(List<ImageCache.ImageEntry> imagelist)
        {
            GLForm iform = new GLForm("Imagesmenu", "Configure Images", new Rectangle(100, 50, 1200, 500), Color.FromArgb(220, 60, 60, 160), Color.Orange, true);

            // provide opening animation
            iform.ScaleWindow = new SizeF(0.0f, 0.0f);
            iform.Animators.Add(new GLControlAnimateScale(10, 300, true, new SizeF(1, 1)));

            // and closing animation
            iform.FormClosing += (f, e) =>
            {
                e.Handled = true;       // stop close
                var ani = new GLControlAnimateScale(10, 200, true, new SizeF(0, 0));       // add a close animation
                ani.FinishAction += (a, c, t) => { iform.ForceClose(); };   // when its complete, force close
                iform.Animators.Add(ani);
            };

            GLScrollPanelScrollBar spanel = new GLScrollPanelScrollBar("ImagesScrollPanel", DockingType.Fill, 0F);
            spanel.EnableHorzScrolling = false;
            iform.Add(spanel);


            GLPanel top = new GLPanel("Paneltop", new Size(0, 32), DockingType.Top, 0F);
            iform.Add(top);

            List<ImageCache.ImageEntry> inbuiltlist = new List<ImageCache.ImageEntry>
            {
                new ImageCache.ImageEntry("EDAstro Indexed Heat Map", @"https://edastro.com/mapcharts/visited-systems-indexedheatmap.png",
                        true,new Point3D(6140,0,18850),new PointF(102300,102300),new Point3D(0,0,0),false,false,0,0.4f),
                new ImageCache.ImageEntry("EDAstro Indexed Log Heat Map", @"https://edastro.com/mapcharts/visited-systems-heatmap.png",
                        true,new Point3D(6140,0,18850),new PointF(102300,102300),new Point3D(0,0,0),false,false,0,0.4f),
                new ImageCache.ImageEntry("EDAstro Indexed Heat Map+Regions", @"https://edastro.com/mapcharts/visited-systems-indexedregions.jpg",
                        true,new Point3D(6140,0,18850),new PointF(102300,102300),new Point3D(0,0,0),false,false,0,0.4f),
                new ImageCache.ImageEntry("EDAstro Indexed Log Heat Map+Regions", @"https://edastro.com/mapcharts/visited-systems-regions.png",
                        true,new Point3D(6140,0,18850),new PointF(102300,102300),new Point3D(0,0,0),false,false,0,0.4f)
            };

            GLComboBox presel = new GLComboBox("Preselects", new Rectangle(40, 4, 240, 24), inbuiltlist.Select(x => x.Name).ToList());

            top.Add(presel);

            map.displaycontrol.AddModalForm(iform);
            iform.FormClosed += (s) => {
                if (iform.DialogResult == GLForm.DialogResultEnum.OK)
                    map.LoadBitmaps();
            };

            int availablewidth = iform.ClientRectangle.Width - spanel.ScrollBarWidth;

            PopulateImagesScrollPanel(spanel, top, imagelist, availablewidth);  // add after iform added to get correct scroll bar width

            presel.SelectedIndexChanged += (s) => {
                imagelist.Add(inbuiltlist[presel.SelectedIndex]);
                PopulateImagesScrollPanel(spanel, null, imagelist, availablewidth);
                spanel.VertScrollPos = int.MaxValue;        // goto bottom
            };
            presel.TabOrder = 0;
            presel.SetFocus();
        }

        private void PopulateImagesScrollPanel( GLScrollPanelScrollBar spanel, GLPanel toppanel, List<ImageCache.ImageEntry> imagelist, int width)
        {
            spanel.SuspendLayout();
            spanel.Remove();

            List<string> resourcenames = new List<string> { "dotted", "dotted2", "dotted1", "_2d", "_3d", "Backwards", "EDSMUnknown" };

            int tabno = 1;

            int leftmargin = 4;
            int vpos = 10;
            int ypad = 10;
            int hpad = 8;
            for( int entry = 0; entry < imagelist.Count; entry++)
            {
                var ie = imagelist[entry];

                var cenable = new GLCheckBox($"En{entry}", new Rectangle(leftmargin, vpos, iconsize, iconsize), "", ie.Enabled);
                cenable.CheckChanged += (s) => ie.Enabled = cenable.Checked;
                spanel.Add(cenable,ref tabno);

                int spaceavailable = width - iconsize * 11 - hpad * 12 - leftmargin * 2;

                var name = new GLTextBoxAutoComplete("name", new Rectangle(cenable.Right + hpad, vpos, spaceavailable/3, iconsize), ie.Name);
                name.TextChanged += (s) => { ie.Name = name.Text; };
                spanel.Add(name, ref tabno);

                spaceavailable -= spaceavailable / 3 + hpad;

                var urlpath = new GLTextBoxAutoComplete("urlpath", new Rectangle(name.Right + hpad, vpos, spaceavailable, iconsize), ie.ImagePathOrURL);
                urlpath.PerformAutoCompleteInThread += (input, sender, set) =>
                {
                    if (input.StartsWith("Resource:", StringComparison.InvariantCultureIgnoreCase))
                        input = input.Substring(9);
                    foreach (var x in resourcenames)
                    {
                        if (x.Contains(input, StringComparison.InvariantCultureIgnoreCase))
                            set.Add("Resource:"+x);
                    }
                };
                
                urlpath.ToolTipText = "Use Resource:name for a resource, Image:name for a EDD icon, \"text\" for text, http:\\pathto for a internet image, or <path> for a local file";
                urlpath.TextChanged += (s) => { ie.ImagePathOrURL = urlpath.Text; };
                spanel.Add(urlpath, ref tabno);

                var ccentre = new GLButton("cent", new Rectangle(urlpath.Right + hpad, vpos, iconsize, iconsize), Properties.Resources._3d, true);
                ccentre.Click += (s, e) => ShowVector3Menu(ie.Centre, true, ccentre.FindScreenCoords(), "Centre", (v) => ie.Centre = v);
                spanel.Add(ccentre, ref tabno);

                var csize = new GLButton("size", new Rectangle(ccentre.Right + hpad, vpos, iconsize, iconsize), Properties.Resources._3d, true);
                csize.Click += (s, e) => ShowVector3Menu(new Point3D(ie.Size.X,ie.Size.Y,0), false, csize.FindScreenCoords(), "Size", (v) => ie.Size = new PointF(v.X,v.Y));
                spanel.Add(csize, ref tabno);

                var crot = new GLButton("rot", new Rectangle(csize.Right + hpad, vpos, iconsize, iconsize), Properties.Resources._3d, true);
                crot.Click += (s, e) => ShowVector3Menu(ie.RotationDegrees, true, crot.FindScreenCoords(), "Rotation Degrees", (v) => ie.RotationDegrees = v);
                spanel.Add(crot, ref tabno);

                var crotaz = new GLCheckBox("rotaz", new Rectangle(crot.Right + hpad, vpos, iconsize, iconsize), "",ie.RotateToViewer );
                crotaz.ToolTipText = "Enables rotation to the viewer azimuth so it faces you";
                crotaz.CheckChanged += (s) => ie.RotateToViewer = crotaz.Checked;
                spanel.Add(crotaz, ref tabno);

                var crotel = new GLCheckBox("rotel", new Rectangle(crotaz.Right + hpad, vpos, iconsize, iconsize), "", ie.RotateElevation );
                crotel.ToolTipText = "Enables rotation to the viewer elevation so it faces you";
                crotel.CheckChanged += (s) => ie.RotateElevation = crotel.Checked;
                spanel.Add(crotel, ref tabno);

                var calscalar = new GLNumberBoxFloat("scalar", new Rectangle(crotel.Right + hpad, vpos, iconsize * 3 / 2, iconsize), ie.AlphaFadeScalar);
                calscalar.ToolTipText = "Alpha scaling by distance from eye. >0 fade out as distance decreases. <0 fade out as distance increase. 0 = fixed fade (determined by position)";
                calscalar.Format = "0.#";
                calscalar.ValueChanged += (s) => { ie.AlphaFadeScalar = calscalar.Value; };
                calscalar.Minimum = 0;
                spanel.Add(calscalar, ref tabno);

                var calpos = new GLNumberBoxFloat("pos", new Rectangle(calscalar.Right + hpad, vpos, iconsize * 3 / 2, iconsize), ie.AlphaFadePosition);
                calpos.ToolTipText = "if scalar != 0, Distance where fade starts/end. If scalar = 0, 0 = no fade, else fade between 0-1";
                calpos.Format = "0.#";
                calpos.ValueChanged += (s) => { ie.AlphaFadePosition = calpos.Value; };
                calpos.Minimum = 0;
                spanel.Add(calpos, ref tabno);

                GLButton delb = new GLButton("Del", new Rectangle(calpos.Right + hpad + hpad, vpos, iconsize, iconsize), Properties.Resources._3d, true);
                delb.Click += (s, e) => {
                    int sp = spanel.VertScrollPos;
                    imagelist.Remove(ie);
                    PopulateImagesScrollPanel(spanel,null,imagelist,width);
                    spanel.VertScrollPos = sp;
                };
                spanel.Add(delb, ref tabno);

                if ( toppanel != null)
                {
                    toppanel.Add(new GLLabel("LEn", new Rectangle(cenable.Left, 4, iconsize + hpad, 20), "En"));
                    toppanel.Add(new GLLabel("Lurl", new Rectangle(urlpath.Left, 4, 200, 20), "URL/Resource/Image/File"));
                    toppanel.Add(new GLLabel("LPos", new Rectangle(ccentre.Left, 4, iconsize + hpad, 20), "Pos"));
                    toppanel.Add(new GLLabel("LSize", new Rectangle(csize.Left, 4, iconsize + hpad, 20), "Size"));
                    toppanel.Add(new GLLabel("LRot", new Rectangle(crot.Left, 4, iconsize + hpad, 20), "Rot"));
                    toppanel.Add(new GLLabel("LDel", new Rectangle(delb.Left, 4, iconsize + hpad, 20), "Del"));
                    toppanel.Add(new GLLabel("LMU", new Rectangle(delb.Right + hpad, 4, iconsize + hpad, 20), "Up"));
                    toppanel.Add(new GLLabel("AZRot", new Rectangle(crotaz.Left , 4, iconsize + hpad, 20), "AZr"));
                    toppanel.Add(new GLLabel("AZEl", new Rectangle(crotel.Left, 4, iconsize + hpad, 20), "ELr"));
                    toppanel.Add(new GLLabel("als", new Rectangle(calscalar.Left, 4, iconsize*3/2 + hpad, 20), "AlSc"));
                    toppanel.Add(new GLLabel("ald", new Rectangle(calpos.Left, 4, iconsize*3/2 + hpad, 20), "AlP/F"));
                }

                if (entry > 0)
                {
                    GLButton upb = new GLButton("Up", new Rectangle(delb.Right + hpad, vpos, iconsize, iconsize), Properties.Resources._3d, true);
                    upb.Click += (s, e) =>
                    {
                        int sp = spanel.VertScrollPos;
                        int entryno = imagelist.IndexOf(ie);
                        if (entryno > 0)
                        {
                            var previous = imagelist[entryno - 1];
                            imagelist.Remove(previous);      // this makes our current be in the previous slot
                            imagelist.Insert(entryno, previous); // and we insert previous into our pos
                        }
                        PopulateImagesScrollPanel(spanel, null, imagelist, width);
                        spanel.VertScrollPos = sp;
                    };
                    spanel.Add(upb, ref tabno);
                }

                vpos += cenable.Height + ypad;
            }

            GLButton add = new GLButton("Add", new Rectangle(leftmargin, vpos, iconsize, iconsize), Properties.Resources._3d, true);
            add.Click += (s, e) =>
            {
                imagelist.Add(new ImageCache.ImageEntry("", "", true, new Point3D(0, 0, 0), new PointF(200, 200), new Point3D(0, 0, 0)));
                PopulateImagesScrollPanel(spanel, null, imagelist, width);
                spanel.VertScrollPos = int.MaxValue;        // goto bottom
            };
            spanel.Add(add, ref tabno);

            GLButton save = new GLButton("Save", new Rectangle(add.Right + hpad, vpos, iconsize*2, iconsize), "Save");
            save.Click += (s, e) =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = "ImageList";
                sfd.DefaultExt = "eddil";
                sfd.Filter = "EDD Image List (*.eddil)|*.eddil|All Files (*.*)|*.*";
                if ( sfd.ShowDialog() == DialogResult.OK)
                {
                    bool success = false;
                    JArray ret = JToken.FromObjectWithError(imagelist, true, membersearchflags: System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Array();
                    if ( ret != null )
                    {
                        var str = ret.ToString(true);
                        success = BaseUtils.FileHelpers.TryWriteToFile(sfd.FileName, str);
                    }
                    if (!success)
                    {
                        GLMessageBox.Show("m1", spanel, new Point(int.MinValue, 0), $"Failed to save {sfd.FileName}", "Error",
                                    GLMessageBox.MessageBoxButtons.OK);
                    }
                }

            };
            spanel.Add(save, ref tabno);

            GLButton load = new GLButton("Load", new Rectangle(save.Right + hpad, vpos, iconsize*2, iconsize), "Load");
            load.Click += (s, e) =>
            {
                OpenFileDialog sfd = new OpenFileDialog();
                sfd.FileName = "ImageList";
                sfd.DefaultExt = "eddil";
                sfd.Filter = "EDD Image List (*.eddil)|*.eddil|All Files (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    bool success = false;
                    string str = BaseUtils.FileHelpers.TryReadAllTextFromFile(sfd.FileName);
                    if ( str != null)
                    {
                        JToken json = QuickJSON.JToken.Parse(str, QuickJSON.JToken.ParseOptions.CheckEOL);
                        if (json != null)
                        {
                            var list = json.ToObject<List<ImageCache.ImageEntry>>();
                            if (list != null)
                            {
                                imagelist = list;
                                success = true;
                            }
                        }
                    }

                    if (success)
                    {
                        PopulateImagesScrollPanel(spanel, null, imagelist, width);
                    }
                    else
                    {
                        GLMessageBox.Show("m1", spanel, new Point(int.MinValue, 0), $"Failed to load {sfd.FileName}", "Error",
                                    GLMessageBox.MessageBoxButtons.OK);
                    }
                }
            };
            spanel.Add(load, ref tabno);

            GLButton ok = new GLButton("OK", new Rectangle(width - leftmargin - 80, vpos, 80, iconsize), "OK");
            ok.Click += (s, e) =>
            {
                images.SetImageList(imagelist);
                spanel.FindForm().DialogResult = GLForm.DialogResultEnum.OK;
                spanel.FindForm().Close();
            };
            spanel.Add(ok, ref tabno);

            spanel.ResumeLayout();
        }

        public void ShowVector3Menu(Point3D value,bool vector3, Point pos, string name, Action<Point3D> onok)
        {
            GLFormVector3 iform = new GLFormVector3("Vectormenu", name, new Vector3(value.X, value.Y, value.Z), new Rectangle(pos, new Size(250, 150)), vector2: !vector3);
            iform.DialogResultChanged += (s,e) => {
                System.Diagnostics.Debug.WriteLine($"ON OK {iform.Value.X} {iform.Value.Y} {iform.Value.Z}");
                onok(new Point3D(iform.Value.X, iform.Value.Y, vector3 ? iform.Value.Z : 0)); 
            };
            map.displaycontrol.AddModalForm(iform);
        }


        public void UpdateCoords(GLOFC.Controller.Controller3D pc)
        {
            if (status != null)
            {
                status.Text = pc.PosCamera.LookAt.X.ToStringInvariant("N1") + " ," + pc.PosCamera.LookAt.Y.ToStringInvariant("N1") + " ,"
                         + pc.PosCamera.LookAt.Z.ToStringInvariant("N1") + " Dist " + pc.PosCamera.EyeDistance.ToStringInvariant("N1") + " Eye " +
                         pc.PosCamera.EyePosition.X.ToStringInvariant("N1") + " ," + pc.PosCamera.EyePosition.Y.ToStringInvariant("N1") + " ," + pc.PosCamera.EyePosition.Z.ToStringInvariant("N1");
                //+ " ! " + pc.PosCamera.CameraDirection + " R " + pc.PosCamera.CameraRotation;
            }
        }

        static void Theme(GLBaseControl ctrl)      // run on each control during add, theme it
        {
            //System.Diagnostics.Debug.WriteLine($"Theme {ctrl.GetType().Name} {ctrl.Name}");

            Color formback = Color.FromArgb(220, 60, 60, 70);
            Color buttonface = Color.FromArgb(255, 128, 128, 128);
            Color texc = Color.Orange;

            var but = ctrl as GLButton;
            if (but != null)
            {
                but.ButtonFaceColor = buttonface;
                but.ForeColor = texc;
                but.BackColor = formback;
                but.BorderColor = Color.FromArgb(255,90,90,90);
            }

            var cb = ctrl as GLCheckBox;
            if (cb != null)
            {
                cb.BackColor = formback;
                cb.ButtonFaceColor = buttonface;
                cb.CheckBoxInnerColor = texc;
                cb.TickBoxReductionRatio = 0.7f;
            }
            var cmb = ctrl as GLComboBox;
            if (cmb != null)
            {
                cmb.BackColor = formback;
                cmb.ForeColor = cmb.DropDownForeColor = texc;
                cmb.FaceColor = cmb.DropDownBackgroundColor = buttonface;
                cmb.BorderColor = formback;
            }

            var dt = ctrl as GLDateTimePicker;
            if (dt != null)
            {
                dt.BackColor = buttonface;
                dt.ForeColor = texc;
                dt.Calendar.ButLeft.ForeColor = dt.Calendar.ButRight.ForeColor = texc;
                dt.SelectedColor = Color.FromArgb(255, 160, 160, 160);
            }

            var fr = ctrl as GLForm;
            if (fr != null)
            {
                fr.BackColor = formback;
                fr.ForeColor = texc;
            }

            var tb = ctrl as GLMultiLineTextBox;    // also autocomplete text box
            if (tb != null)
            {
                tb.BackColor = formback;
                tb.ForeColor = texc;
                if (!(tb.Parent is GLFormConfigurable))     // this is to stop themeing the box around the warning text
                {
                    tb.BorderColor = Color.Gray;
                    tb.BorderWidth = 1;
                }
            }

            var lb = ctrl as GLLabel;
            if (lb != null)
            {
                lb.ForeColor = texc;
            }

            Color cmbck = Color.FromArgb(255, 128, 128, 128);

            var ms = ctrl as GLMenuStrip;
            if (ms != null)
            {
                ms.BackColor = cmbck;
                ms.IconStripBackColor = cmbck.Multiply(1.2f);
            }
            var mi = ctrl as GLMenuItem;
            if (mi != null)
            {
                mi.BackColor = cmbck;
                mi.ButtonFaceColor = cmbck;
                mi.ForeColor = texc;
                mi.BackDisabledScaling = 1.0f;
            }

            var gb = ctrl as GLGroupBox;
            if (gb != null)
            {
                gb.BackColor = Color.Transparent;
                gb.ForeColor = Color.Orange;
            }

            var flp = ctrl as GLFlowLayoutPanel;
            if (flp != null)
            {
                flp.BackColor = formback;
            }

            var sp = ctrl as GLScrollPanelScrollBar;
            if (sp != null)
            {
                sp.BackColor = formback;
            }

            var p = ctrl as GLPanel;
            if (p != null)
            {
                p.BackColor = formback;
            }


            //{
            //    float[][] colorMatrixElements = {
            //               new float[] {0.5f,  0,  0,  0, 0},        // red scaling factor of 0.5
            //               new float[] {0,  0.5f,  0,  0, 0},        // green scaling factor of 1
            //               new float[] {0,  0,  0.5f,  0, 0},        // blue scaling factor of 1
            //               new float[] {0,  0,  0,  1, 0},        // alpha scaling factor of 1
            //               new float[] {0.0f, 0.0f, 0.0f, 0, 1}};    // three translations of 

            //    var colormap1 = new System.Drawing.Imaging.ColorMap();
            //    cb.SetDrawnBitmapUnchecked(new System.Drawing.Imaging.ColorMap[] { colormap1 }, colorMatrixElements);
            //}
        }


    }
}
