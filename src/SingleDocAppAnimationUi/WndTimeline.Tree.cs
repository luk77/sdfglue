//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation.Ui
{
    // The tree on the left of the timeline: sections -> targets -> groups of properties -> properties (and the child
    // targets). The rows are built every frame from what the host shows; only the expanded branches are built.
    public partial class WndTimeline
    {
        private enum KeyFilter
        {
            All,
            Animated,
            Selected,
        }

        private readonly List<AnimRow>      rows_       = new List<AnimRow>();
        private readonly HashSet<string>    expanded_   = new HashSet<string>();
        private KeyFilter                   filter_;
        private string                      search_     = "";

        protected AnimRow? FindRow(string id)
        {
            return id.Length == 0 ? null : rows_.FirstOrDefault(r => r.Id == id);
        }

        private void BuildRows(AnimEditorController ed, IAnimRowSource source)
        {
            rows_.Clear();
            string search = search_.Trim();
            foreach (AnimSection section in source.Sections)
            {
                if (filter_ == KeyFilter.Animated && section.Container.Tracks.Count == 0 && !section.Animated)
                    continue;
                if (filter_ == KeyFilter.Selected && !section.Active && !section.Targets.Any(source.IsSelected) && !ed.SelectedRows.Any(r => r.ContainerId == section.Container.Id))
                    continue;

                AnimRow row = new AnimRow { Kind = AnimRowKind.Section, Depth = 0, Id = section.Id, Label = section.Label, Section = section };
                row.Expandable = section.Targets.Count > 0;
                row.Expanded = row.Expandable && (search.Length > 0 || expanded_.Contains(row.Id));
                int at = rows_.Count;
                rows_.Add(row);
                if (!row.Expanded)
                    continue;

                bool any = false;
                foreach (IAnimTarget target in section.Targets)
                    any |= AddTargetRows(ed, source, section, row.Id, target, 1, search);
                if (search.Length > 0 && !any && !row.Label.Contains(search, StringComparison.OrdinalIgnoreCase))
                    rows_.RemoveRange(at, rows_.Count - at);
            }
        }

        // the rows of a target; false: nothing was shown
        private bool AddTargetRows(AnimEditorController ed, IAnimRowSource source, AnimSection section, string parentId, IAnimTarget target, int depth, string search)
        {
            if (target.Id == 0)
                return false;
            if (filter_ == KeyFilter.Animated && !section.Container.HasTracks(target.Id))
                return false;
            if (filter_ == KeyFilter.Selected && !source.IsSelected(target) && !ed.SelectedRows.Any(r => r.TargetId == target.Id && r.ContainerId == section.Container.Id))
                return false;

            AnimRow row = new AnimRow { Kind = AnimRowKind.Target, Depth = depth, Id = parentId + "/" + target.Id, Label = target.Label, Section = section, Target = target, Expandable = true };
            row.Expanded = search.Length > 0 || expanded_.Contains(row.Id);
            int at = rows_.Count;
            rows_.Add(row);
            if (!row.Expanded)
                return true;

            bool any = EmitItems(AnimGroup.Build(target), row, depth + 1, search);
            foreach (IAnimTarget child in target.Children)
                any |= AddTargetRows(ed, source, section, row.Id, child, depth + 1, search);
            if (search.Length > 0 && !any && !row.Label.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                rows_.RemoveRange(at, rows_.Count - at);
                return false;
            }
            return true;
        }

        // the rows of the items of a group; false: nothing was shown
        private bool EmitItems(AnimGroup group, AnimRow parent, int depth, string search)
        {
            bool any = false;
            IAnimTarget target = parent.Target!;
            foreach (object item in group.Items)
            {
                if (item is IAnimProperty p)
                {
                    AnimTrack? track = parent.Container.FindTrack(target.Id, p.Path);
                    if (filter_ == KeyFilter.Animated && track == null)
                        continue;
                    string label = Strings.Property(p.Label);
                    if (search.Length > 0 && !label.Contains(search, StringComparison.OrdinalIgnoreCase) && !Strings.Property(group.Label).Contains(search, StringComparison.OrdinalIgnoreCase)
                        && !parent.Label.Contains(search, StringComparison.OrdinalIgnoreCase))
                        continue;
                    rows_.Add(new AnimRow
                    {
                        Kind = AnimRowKind.Property, Depth = depth, Id = parent.Id + "/" + p.Path, Label = label, Section = parent.Section,
                        Target = target, Property = p, Track = track,
                    });
                    any = true;
                    continue;
                }

                AnimGroup child = (AnimGroup)item;
                if (filter_ == KeyFilter.Animated && !parent.Container.TracksOf(target.Id).Any(child.HasTrack))
                    continue;
                AnimRow row = new AnimRow
                {
                    Kind = AnimRowKind.Group, Depth = depth, Id = parent.Id + "/" + child.Path, Label = Strings.Property(child.Label), Section = parent.Section,
                    Target = target, Group = child, Expandable = true,
                };
                row.Expanded = search.Length > 0 || expanded_.Contains(row.Id);
                int at = rows_.Count;
                rows_.Add(row);
                bool shown = row.Expanded ? EmitItems(child, row, depth + 1, search) : true;
                if (!shown || (search.Length > 0 && !row.Label.Contains(search, StringComparison.OrdinalIgnoreCase) && rows_.Count == at + 1))
                {
                    rows_.RemoveRange(at, rows_.Count - at);
                    continue;
                }
                any = true;
            }
            return any;
        }

        // the state of the key button of a row at the playhead
        private static AnimKeyState StateOf(AnimEditorController ed, AnimRow row)
        {
            if (row.Target == null)
                return AnimKeyState.None;
            if (row.Kind == AnimRowKind.Property)
                return row.Track == null ? AnimKeyState.None : row.Track.KeyAt(ed.Transport.KeyTime) != null ? AnimKeyState.OnKey : AnimKeyState.Animated;
            return row.Tracks.Count == 0 ? AnimKeyState.None : ed.StateOf(row.Container, row.Target.Id, row.KeyPaths);
        }
    }
}
