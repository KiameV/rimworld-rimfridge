using RimWorld;

namespace RimFridge
{
    class RimFridge_Building : Building_Storage
    {
        public RimFridge_Building() : base() { }

        /*public override IEnumerable<Gizmo> GetGizmos()
        {
            List<Gizmo> l = new List<Gizmo>(base.GetGizmos());
            return SaveStorageSettingsGizmoUtil.AddSaveLoadGizmos(l, "fridge", this.settings.filter);
        }*/
    }
}
