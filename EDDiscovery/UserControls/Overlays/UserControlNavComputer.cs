using EliteDangerousCore;
using EliteDangerousCore.DB;
using EliteDangerousCore.JournalEvents;
using EMK.LightGeometry;
using ExtendedControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EDDiscovery.UserControls
{
    public partial class UserControlNavComputer : UserControlCommonBase
    {
        private const double FUEL_USE_FACTOR = 1.1; // let's be a bit pessimistic with the calculated fuel use.

        public enum NavPointer
        {
            Past,
            Current,
            Future,
        }
        public struct NavRenderContext
        {
            public HistoryEntry LastEntry { get; private set; }
            public ExtPictureBox PictureBox { get; private set; }
            public Font DisplayFont { get; private set; }
            public Color ClrText { get; private set; }
            public Color ClrBack { get; private set; }
            public NavPointer Pointer { get; set; }
            public double Fuel { get; set; }
            public Point Pos { get; set; }

            public NavRenderContext(
                UserControlNavComputer owner,
                HistoryEntry he,
                Point pos)
            {
                LastEntry = he;
                PictureBox = owner.pictureBox;
                DisplayFont = owner.DisplayFont;
                ClrText = owner.IsTransparent ? owner.discoveryform.theme.SPanelColor : owner.discoveryform.theme.LabelColor;
                ClrBack = owner.IsTransparent ? Color.Transparent : owner.BackColor;
                Pointer = NavPointer.Past;
                Pos = pos;
                Fuel = he.ShipInformation != null ? he.ShipInformation.FuelLevel : 0;
            }
        }
        public class NavJump
        {
            public double Distance { get; private set; }
            public double FuelCost { get; private set; }

            public NavJump(ISystem from, ISystem to)
            {
                Distance = from.Distance(to);
            }

            public bool Calculate(HistoryEntry he)
            {
                double oldFuelCost = FuelCost;

                ShipInformation si = he != null ? he.ShipInformation : null;
                EliteDangerousCalculations.FSDSpec fdSpec = si != null ? si.GetFSDSpec() : null;
                FuelCost = fdSpec != null ? fdSpec.FuelUse(he.MaterialCommodity.CargoCount, si.UnladenMass, Distance) * FUEL_USE_FACTOR : -1;

                return FuelCost != oldFuelCost;
            }

            public void Render(NavEntry from, ref NavRenderContext ctx)
            {
                Color clrTrans = Color.FromArgb((int)((float)ctx.ClrText.A * 0.25f), ctx.ClrText);

                ctx.PictureBox.AddTextAutoSize(
                    new Point(ctx.Pos.X + 35, ctx.Pos.Y),
                    new Size(10000, 10000),
                    "Dist:",
                    ctx.DisplayFont,
                    clrTrans,
                    ctx.ClrBack,
                    1.0f);

                ctx.PictureBox.AddTextAutoSize(
                    new Point(ctx.Pos.X + 65, ctx.Pos.Y),
                    new Size(10000, 10000),
                    $"{Distance:##0.00} LY",
                    ctx.DisplayFont,
                    ctx.ClrText,
                    ctx.ClrBack,
                    1.0f);

                ctx.Pos = new Point(ctx.Pos.X, ctx.Pos.Y + 15);

                if (ctx.Pointer != NavPointer.Past &&
                    FuelCost >= 0)
                {
                    Color clrSccop = from.Scoopable ? Color.Green : Color.Red;
                    ctx.PictureBox.AddTextAutoSize(
                            new Point(ctx.Pos.X + 5, ctx.Pos.Y),
                            new Size(10000, 10000),
                            from.StarClass,
                            ctx.DisplayFont,
                            clrSccop,
                            ctx.ClrBack,
                            1.0F);

                    ctx.PictureBox.AddTextAutoSize(
                        new Point(ctx.Pos.X + 35, ctx.Pos.Y),
                        new Size(10000, 10000),
                        "Fuel:",
                        ctx.DisplayFont,
                        clrTrans,
                        ctx.ClrBack,
                        1.0f);

                    double preJumpFuel = ctx.Fuel;
                    ctx.Fuel = Math.Max(ctx.Fuel - FuelCost, 0);
                    Color clrFuel = ctx.Fuel > 0 ? ctx.ClrText : clrTrans;
                    ctx.PictureBox.AddTextAutoSize(
                        new Point(ctx.Pos.X + 65, ctx.Pos.Y),
                        new Size(10000, 10000),
                        $"Cost: {FuelCost:##0.00} | Tank: {preJumpFuel:##0.00} => {ctx.Fuel:##0.00}",
                        ctx.DisplayFont,
                        clrFuel,
                        ctx.ClrBack,
                        1.0f);

                    ctx.Pos = new Point(ctx.Pos.X, ctx.Pos.Y + 15);
                }
            }
        }
        public class NavEntry
        {
            private static HashSet<string> s_scoopable = new HashSet<string>
            {
                "K",
                "G",
                "B",
                "F",
                "O",
                "A",
                "M",
            };

            public ISystem System { get; private set; }
            public long? SystemAddress { get; private set; }
            public string SystemName { get; private set; }
            public string StarClass { get; private set; }
            public Vector3D Position { get; private set; }
            public bool Scoopable { get { return s_scoopable.Contains(StarClass); } }
            public NavJump Jump { get; set; }

            public NavEntry(ISystem system)
            {
                System = system;
                SystemAddress = system.SystemAddress;
                SystemName = system.Name;
                StarClass = "-";
                Position = new Vector3D(system.X, system.Y, system.Z);
            }

            public NavEntry(EliteDangerousCore.JournalEvents.JournalNavRoute.NavRouteEntry jeNavRouteEntry)
            {
                System = SystemCache.FindSystem(jeNavRouteEntry.StarSystem);
                SystemAddress = jeNavRouteEntry.SystemAddress;
                SystemName = jeNavRouteEntry.StarSystem;
                StarClass = jeNavRouteEntry.StarClass;
                Position = new Vector3D(jeNavRouteEntry.StarPos.X, jeNavRouteEntry.StarPos.Y, jeNavRouteEntry.StarPos.Z);
            }

            public void Render(ref NavRenderContext ctx)
            {
                if (ctx.Pointer == NavPointer.Current)
                {
                    ctx.PictureBox.AddTextAutoSize(
                        ctx.Pos,
                        new Size(10000, 10000),
                        ">",
                        ctx.DisplayFont,
                        ctx.ClrText,
                        ctx.ClrBack,
                        1.0F);
                }

                ctx.PictureBox.AddTextAutoSize(
                        new Point(ctx.Pos.X + 10, ctx.Pos.Y),
                        new Size(10000, 10000),
                        SystemName,
                        ctx.DisplayFont,
                        ctx.ClrText,
                        ctx.ClrBack,
                        1.0F);


                ctx.Pos = new Point(ctx.Pos.X, ctx.Pos.Y + 15);
                if (Jump != null)
                {
                    Jump.Render(this, ref ctx);
                }
            }
        }

        [Flags]
        public enum Configuration
        {
            showPastSystems = 1,
            drawGraph = 2,
        };

        private string DbSave { get { return DBName("NavComputer"); } }
        private List<NavEntry> route = new List<NavEntry>();

        public HistoryEntry LastHE { get; private set; }
        public Font DisplayFont { get; private set; }
        public Configuration Config { get; private set; } = (Configuration)(Configuration.showPastSystems | Configuration.drawGraph);

        public UserControlNavComputer()
        {
            InitializeComponent();
        }

        public override void Init()
        {
            Config = (Configuration)EliteDangerousCore.DB.UserDatabase.Instance.GetSettingInt(DbSave + "Config", (int)Config);

            DisplayFont = discoveryform.theme.GetFont;

            discoveryform.OnNewCalculatedRoute += Discoveryform_OnNewCalculatedRoute;
            discoveryform.OnNewJournalEntry += Discoveryform_OnNewJournalEntry;
            discoveryform.OnHistoryChange += Discoveryform_OnHistoryChange;
            discoveryform.OnNewEntry += Discoveryform_OnNewEntry;
            discoveryform.OnThemeChanged += Discoveryform_OnThemeChanged;

            SizeChanged += UserControlNavComputer_SizeChanged;

            BaseUtils.Translator.Instance.Translate(this);
            BaseUtils.Translator.Instance.Translate(contextMenuStrip, this);
        }

        public override void Closing()
        {
            discoveryform.OnNewCalculatedRoute -= Discoveryform_OnNewCalculatedRoute;
            discoveryform.OnNewJournalEntry -= Discoveryform_OnNewJournalEntry;
            discoveryform.OnHistoryChange -= Discoveryform_OnHistoryChange;
            discoveryform.OnNewEntry -= Discoveryform_OnNewEntry;
            discoveryform.OnThemeChanged -= Discoveryform_OnThemeChanged;

            SizeChanged -= UserControlNavComputer_SizeChanged;

            EliteDangerousCore.DB.UserDatabase.Instance.PutSettingInt(DbSave + "Config", (int)Config);
        }

        public override void InitialDisplay()
        {
            HistoryEntry he = discoveryform.history.GetLast;
            if (he == null)
            {
                return;
            }

            if (InitRouteIfEmpty())
            {
                UpdateFuelUsage(he);
                Display(he);
            }
        }

        private void UserControlNavComputer_SizeChanged(object sender, EventArgs e)
        {
            if ((Config & Configuration.drawGraph) == Configuration.drawGraph)
            {
                Display(LastHE);
            }
        }

        private void Discoveryform_OnNewEntry(HistoryEntry he, HistoryList hl)
        {
            UpdateFuelUsage(he);
            Display(he);
        }

        private void Discoveryform_OnThemeChanged()
        {
            Display(LastHE);
        }

        private void Discoveryform_OnHistoryChange(HistoryList hl)
        {
            HistoryEntry he = hl.GetLast;
            if (he == null)
            {
                return;
            }

            if (InitRouteIfEmpty())
            {
                UpdateFuelUsage(he);
                Display(he);
            }
        }

        private void Discoveryform_OnNewJournalEntry(JournalEntry je)
        {
            if (je is EliteDangerousCore.JournalEvents.JournalNavRoute jeRoute)
            {
                if (UpdateRoute(jeRoute))
                {
                    UpdateFuelUsage(LastHE);
                    Display(LastHE);
                }
            }
        }

        private void Discoveryform_OnNewCalculatedRoute(List<ISystem> obj)
        {
            if (UpdateRoute(obj))
            {
                UpdateFuelUsage(LastHE);
                Display(LastHE);
            }
        }

        public override bool SupportTransparency { get { return true; } }
        public override void SetTransparency(bool on, Color curcol)
        {
            pictureBox.BackColor = this.BackColor = curcol;
            Display(LastHE);
        }

        void FlipConfig(Configuration item, bool ch, bool redisplay = false)
        {
            if (ch)
            {
                Config = (Configuration)((int)Config | (int)item);
            }
            else
            {
                Config = (Configuration)((int)Config & ~(int)item);
            }
        }

        private void miPreviousSystems_Click(object sender, EventArgs e)
        {
            FlipConfig(Configuration.showPastSystems, ((ToolStripMenuItem)sender).Checked, true);
            Display(LastHE);
        }

        private void miShowGraph_Click(object sender, EventArgs e)
        {
            FlipConfig(Configuration.drawGraph, ((ToolStripMenuItem)sender).Checked, true);
            Display(LastHE);
        }

        private void Display(HistoryList hl)            // when user clicks around..  HE may be null here
        {
            Display(hl.GetLast);
        }

        void Display(HistoryEntry he)
        {
            LastHE = he;

            pictureBox.ClearImageList();
            if (LastHE != null)
            {
                Render();
            }
            pictureBox.Render();
        }

        private bool InitRouteIfEmpty()
        {
            if (route.Count < 1)
            {
                JournalNavRoute jeRoute = discoveryform.history.GetLastHistoryEntry(x => x.EntryType == JournalTypeEnum.NavRoute)?.journalEntry as JournalNavRoute;
                return UpdateRoute(jeRoute);
            }
            return false;
        }

        private bool UpdateRoute(JournalNavRoute jeRoute)
        {
            bool changed = route.Count > 0 ||
                (jeRoute != null && jeRoute.Route.Length > 0);

            route.Clear();
            int n = jeRoute != null ? jeRoute.Route.Length : 0;
            for (int i = 0; i < n; ++i)
            {
                PushNavEntry(jeRoute.Route[i]);
            }

            return changed;
        }

        private bool UpdateRoute(List<ISystem> routeList)
        {
            bool changed = route.Count > 0 ||
                (routeList != null && routeList.Count > 0);

            route.Clear();
            int n = routeList != null ? routeList.Count : 0;
            for (int i = 0; i < n; ++i)
            {
                PushNavEntry(routeList[i]);
            }

            return changed;
        }

        private void PushNavEntry(ISystem system)
        {
            PushNavEntry(new NavEntry(system));
        }

        private void PushNavEntry(JournalNavRoute.NavRouteEntry jeNavEntry)
        {
            PushNavEntry(new NavEntry(jeNavEntry));
        }

        private void PushNavEntry(NavEntry entry)
        {
            NavEntry prev = route.Count > 0 ? route[route.Count - 1] : null;
            if (prev != null)
            {
                prev.Jump = new NavJump(prev.System, entry.System);
            }
            route.Add(entry);
        }

        private bool UpdateFuelUsage(HistoryEntry he)
        {
            bool changed = false;

            for (int i = 0; i < route.Count; ++i)
            {
                if (route[i].Jump != null)
                {
                    changed |= route[i].Jump.Calculate(he);
                }
            }

            return changed;
        }

        private void Render()
        {
            if (route.Count > 0)
            {
                NavRenderContext ctx = new NavRenderContext(
                    this,
                    LastHE,
                    new Point(10, 10));

                RenderSummary(ref ctx);
                RenderRoute(ref ctx);
                RenderGraph(ref ctx);
            }
        }

        private void RenderSummary(ref NavRenderContext ctx)
        {
            Color clrTrans = Color.FromArgb((int)((float)ctx.ClrText.A * 0.5f), ctx.ClrText);

            pictureBox.AddTextAutoSize(
                new Point(ctx.Pos.X, ctx.Pos.Y),
                new Size(10000, 10000),
                "Journey:",
                ctx.DisplayFont,
                clrTrans,
                ctx.ClrBack,
                1.0F);

            int nJumps = 0;
            double dist = 0;
            int nJumpsTravelled = 0;
            double distTravelled = 0;
            bool past = true;
            for (int i = 0; i < route.Count; ++i)
            {
                if (route[i].Jump != null)
                {
                    ++nJumps;
                    dist += route[i].Jump.Distance;
                    if (route[i].SystemAddress == LastHE.System.SystemAddress)
                    {
                        past = false;
                    }
                    if (past)
                    {
                        ++nJumpsTravelled;
                        distTravelled += route[i].Jump.Distance;
                    }
                }
            }
            ExtPictureBox.ImageElement txtFrom = pictureBox.AddTextAutoSize(
                new Point(ctx.Pos.X + 70, ctx.Pos.Y),
                new Size(10000, 10000),
                $"{route[0].SystemName}",
                ctx.DisplayFont,
                ctx.ClrText,
                ctx.ClrBack,
                1.0F);
            ExtPictureBox.ImageElement txtArrow = pictureBox.AddTextAutoSize(
                new Point(txtFrom.Location.Right, ctx.Pos.Y),
                new Size(10000, 10000),
                "=>",
                ctx.DisplayFont,
                clrTrans,
                ctx.ClrBack,
                1.0F);
            ExtPictureBox.ImageElement txtTo = pictureBox.AddTextAutoSize(
                new Point(txtArrow.Location.Right, ctx.Pos.Y),
                new Size(10000, 10000),
                $"{route[route.Count - 1].SystemName}",
                ctx.DisplayFont,
                ctx.ClrText,
                ctx.ClrBack,
                1.0F);
            pictureBox.AddTextAutoSize(
                new Point(txtTo.Location.Right, ctx.Pos.Y),
                new Size(10000, 10000),
                $" {dist:0.00} LY    {nJumps} Jumps",
                ctx.DisplayFont,
                clrTrans,
                ctx.ClrBack,
                1.0F);

            if (!past)
            {
                ctx.Pos = new Point(ctx.Pos.X, ctx.Pos.Y + 15);

                pictureBox.AddTextAutoSize(
                    new Point(ctx.Pos.X, ctx.Pos.Y),
                    new Size(10000, 10000),
                    "Travelled:",
                    ctx.DisplayFont,
                    clrTrans,
                    ctx.ClrBack,
                    1.0F);

                ExtPictureBox.ImageElement txtDistTravelled = pictureBox.AddTextAutoSize(
                    new Point(ctx.Pos.X + 70, ctx.Pos.Y),
                    new Size(10000, 10000),
                    $"{distTravelled:0.00} LY ",
                    ctx.DisplayFont,
                    clrTrans,
                    ctx.ClrBack,
                    1.0F);
                ExtPictureBox.ImageElement txtPercentageDistTravelled = pictureBox.AddTextAutoSize(
                    new Point(txtDistTravelled.Location.Right, ctx.Pos.Y),
                    new Size(10000, 10000),
                    $"({((distTravelled / dist) * 100.0):0.0}%)",
                    ctx.DisplayFont,
                    ctx.ClrText,
                    ctx.ClrBack,
                    1.0F);
                ExtPictureBox.ImageElement txtJumpsTravelled = pictureBox.AddTextAutoSize(
                    new Point(txtPercentageDistTravelled.Location.Right, ctx.Pos.Y),
                    new Size(10000, 10000),
                    $"{nJumpsTravelled} Jumps ",
                    ctx.DisplayFont,
                    clrTrans,
                    ctx.ClrBack,
                    1.0F);
                ExtPictureBox.ImageElement txtPercentageJumpsTravelled = pictureBox.AddTextAutoSize(
                    new Point(txtJumpsTravelled.Location.Right, ctx.Pos.Y),
                    new Size(10000, 10000),
                    $"({(((double)nJumpsTravelled / (double)nJumps) * 100.0):0.0}%)",
                    ctx.DisplayFont,
                    ctx.ClrText,
                    ctx.ClrBack,
                    1.0F);
            }

            ctx.Pos = new Point(ctx.Pos.X + 10, ctx.Pos.Y + 20);
        }

        private void RenderRoute(ref NavRenderContext ctx)
        {
            for (int i = 0; i < route.Count; ++i)
            {
                NavEntry entry = route[i];
                if (entry.SystemAddress == LastHE.System.SystemAddress)
                {
                    ctx.Pointer = NavPointer.Current;
                }
                else if (ctx.Pointer == NavPointer.Current)
                {
                    ctx.Pointer = NavPointer.Future;
                }

                if ((Config & Configuration.showPastSystems) == Configuration.showPastSystems ||
                    ctx.Pointer != NavPointer.Past)
                {
                    entry.Render(ref ctx);
                }
            }
        }

        private void RenderGraph(ref NavRenderContext ctx)
        {
            Rectangle canvas = new Rectangle(
                310, 10,
                Width - 320, Height - 20);

            if ((Config & Configuration.drawGraph) == Configuration.drawGraph &&
                canvas.Width > 100 && canvas.Height > 100)
            {
                Color clrFull = ctx.ClrText;
                Color clrTrans = Color.FromArgb((int)((float)ctx.ClrText.A * 0.5f), ctx.ClrText);

                Vector3D extents = new Vector3D(
                    canvas.Width,
                    canvas.Height,
                    1);
                Vector3D min = new Vector3D(
                    double.MaxValue, 
                    double.MaxValue, 
                    double.MaxValue);
                Vector3D max = new Vector3D(
                    double.MinValue, 
                    double.MinValue, 
                    double.MinValue);

                bool past = true;
                for (int i = 0; i < route.Count; ++i)
                {
                    if (route[i].SystemAddress == ctx.LastEntry.System.SystemAddress)
                    {
                        past = false;
                    }
                    if ((Config & Configuration.showPastSystems) == Configuration.showPastSystems ||
                        !past)
                    {
                        GrowBounds(
                        ref min,
                        ref max,
                        route[i].Position);
                    }
                }

                past = true;
                for (int i = 0; i < route.Count; ++i)
                {
                    if ((Config & Configuration.showPastSystems) == Configuration.showPastSystems ||
                        !past)
                    {
                        ctx.PictureBox.AddOwnerDraw((g, ie) =>
                        {
                            Tuple<int, bool> data = ie.Tag as Tuple<int, bool>;
                            int idx = data.Item1;
                            bool pastEntry = data.Item2;
                            using (Brush bScoopable = new SolidBrush(Color.Green))
                            using (Brush bUnscoopable = new SolidBrush(Color.Red))
                            using (Pen pPast = new Pen(discoveryform.theme.VisitedSystemColor, 1.0f))
                            using (Pen pFuture = new Pen(discoveryform.theme.NonVisitedSystemColor, 2.0f))
                            {
                                Vector3D pos = Map(min, max, extents, route[idx].Position);

                                if (idx > 0)
                                {
                                    Vector3D prev = Map(min, max, extents, route[idx - 1].Position);

                                    Pen pLine = pastEntry ? pPast : pFuture;
                                    g.DrawLine(
                                        pLine,
                                        canvas.X + (float)prev.DX, canvas.Y + (float)prev.DY,
                                        canvas.X + (float)pos.DX, canvas.Y + (float)pos.DY);
                                }

                                Brush bStar = route[idx].Scoopable ? bScoopable : bUnscoopable;

                                g.FillEllipse(
                                    bStar,
                                    canvas.X + (float)pos.DX - 1.0f, canvas.Y + (float)pos.DY - 1.0f,
                                    3.0f, 3.0f);
                            }
                        }, canvas, Tuple.Create<int, bool>(i, past));

                        if (route[i].SystemAddress == ctx.LastEntry.System.SystemAddress)
                        {
                            past = false;
                        }

                        Vector3D posSystem = Map(min, max, extents, route[i].Position);
                        posSystem.DY -= 15;
                        if (posSystem.DY < 0) { posSystem.DY += 30; }
                        if (posSystem.DX < 50) { posSystem.DX = 50; }
                        if (posSystem.DX > canvas.Width - 50) { posSystem.DX = canvas.Width - 50; }
                        ctx.PictureBox.AddTextCentred(
                            new Point((int)posSystem.DX + canvas.X, (int)posSystem.DY + canvas.Y),
                            new Size(100, 20),
                            route[i].SystemName,
                            ctx.DisplayFont,
                            ctx.ClrText,
                            Color.Transparent,
                            1.0f);
                    }
                    else if (route[i].SystemAddress == ctx.LastEntry.System.SystemAddress)
                    {
                        past = false;
                    }
                }
            }
        }

        private void GrowBounds(ref Vector3D min, ref Vector3D max, Vector3D point)
        {
            for (int i = 0; i < 3; ++i)
            {
                min[i] = Math.Min(min[i], point[i]);
                max[i] = Math.Max(max[i], point[i]);
            }
        }

        private Vector3D Map(Vector3D min, Vector3D max, Vector3D extents, Vector3D pos)
        {
            double[] v = new double[3];
            for (int i = 0; i < 3; ++i)
            {
                v[i] = pos[i] - min[i];
                v[i] = (v[i] / (max[i] - min[i])) * extents[i];
            }
            return new Vector3D(v);
        }
    }
}
