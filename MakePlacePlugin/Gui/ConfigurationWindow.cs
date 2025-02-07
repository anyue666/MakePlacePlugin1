using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Textures;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using MakePlacePlugin.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using static MakePlacePlugin.MakePlacePlugin;

namespace MakePlacePlugin.Gui
{
    public class ConfigurationWindow : Window<MakePlacePlugin>
    {
        public Configuration Config => Plugin.Config;

        private string CustomTag = string.Empty;
        private readonly Dictionary<uint, uint> iconToFurniture = new();

        private readonly Vector4 PURPLE = new(0.26275f, 0.21569f, 0.56863f, 1f);
        private readonly Vector4 PURPLE_ALPHA = new(0.26275f, 0.21569f, 0.56863f, 0.5f);

        private FileDialogManager FileDialogManager { get; }

        public ConfigurationWindow(MakePlacePlugin plugin) : base(plugin)
        {
            this.FileDialogManager = new FileDialogManager
            {
                AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking,
            };
        }

        protected void DrawAllUi()
        {
            try
            {
                bool windowBeginResult = ImGui.Begin($"{Plugin.Name}", ref WindowVisible, ImGuiWindowFlags.NoScrollWithMouse);
                if (!windowBeginResult)
                {
                    LogError($"ImGui.Begin for {Plugin.Name} returned false. Continuing to draw.");
                }

                DrawSettingsRegion();
                DrawItemListRegion();

                this.FileDialogManager.Draw();

                ImGui.End();
            }
            catch (Exception ex)
            {
                LogError($"Exception in DrawAllUi: {ex.Message}", ex.StackTrace);
            }
        }

        private void DrawSettingsRegion()
        {
            try
            {
                if (ImGui.BeginChild("##SettingsRegion"))
                {
                    DrawGeneralSettings();
                    ImGui.EndChild();
                }
            }
            catch (Exception ex)
            {
                LogError($"Exception in DrawSettingsRegion: {ex.Message}", ex.StackTrace);
            }
        }

        private void DrawItemListRegion()
        {
            try
            {
                if (ImGui.BeginChild("##ItemListRegion"))
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, PURPLE_ALPHA);
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, PURPLE);
                    ImGui.PushStyleColor(ImGuiCol.HeaderActive, PURPLE);

                    DrawCollapsingHeader("Interior Furniture", "interior", Plugin.InteriorItemList, DrawItemList);
                    DrawCollapsingHeader("Exterior Furniture", "exterior", Plugin.ExteriorItemList, DrawItemList);
                    DrawCollapsingHeader("Interior Fixtures", "interiorFixture", Plugin.Layout.interiorFixture, DrawFixtureList);
                    DrawCollapsingHeader("Exterior Fixtures", "exteriorFixture", Plugin.Layout.exteriorFixture, DrawFixtureList);
                    DrawCollapsingHeader("Unused Furniture", "unused", Plugin.UnusedItemList, (list) => DrawItemList(list, true));

                    ImGui.PopStyleColor(3);
                    ImGui.EndChild();
                }
            }
            catch (Exception ex)
            {
                LogError($"Exception in DrawItemListRegion: {ex.Message}", ex.StackTrace);
            }
        }

        private void DrawCollapsingHeader(string headerText, string id, object itemList, Action<object> drawMethod)
        {
            try
            {
                if (ImGui.CollapsingHeader(headerText, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.PushID(id);
                    drawMethod(itemList);
                    ImGui.PopID();
                }
            }
            catch (Exception ex)
            {
                LogError($"Exception in {headerText} collapsing header: {ex.Message}", ex.StackTrace);
            }
        }

        protected override void DrawUi()
        {
            try
            {
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, PURPLE);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, PURPLE_ALPHA);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, PURPLE_ALPHA);
                ImGui.SetNextWindowSize(new Vector2(530, 450), ImGuiCond.FirstUseEver);

                DrawAllUi();

                ImGui.PopStyleColor(3);
                ImGui.End();
            }
            catch (Exception ex)
            {
                LogError($"Exception in DrawUi: {ex.Message}", ex.StackTrace);
            }
        }

        #region Helper Functions
        public void DrawIcon(ushort icon, Vector2 size)
        {
            try
            {
                if (icon < 65000)
                {
                    var iconTexture = DalamudApi.TextureProvider.GetFromGameIcon(new GameIconLookup(icon));
                    ImGui.Image(iconTexture.GetWrapOrEmpty().ImGuiHandle, size);
                }
            }
            catch (Exception ex)
            {
                LogError($"Exception in DrawIcon: {ex.Message}", ex.StackTrace);
            }
        }
        #endregion

        #region Basic UI

        private void LogLayoutMode()
        {
            if (Memory.Instance.GetCurrentTerritory() == Memory.HousingArea.Island)
            {
                LogError("(Manage Furnishings -> Place Furnishing Glamours)");
            }
            else
            {
                LogError("(Housing -> Indoor/Outdoor Furnishings)");
            }
        }

        private bool CheckModeForSave()
        {
            if (Memory.Instance.IsHousingMode()) return true;

            LogError("Unable to save layouts outside of Layout mode");
            LogLayoutMode();
            return false;
        }

        private bool CheckModeForLoad()
        {
            if (Config.ApplyLayout && !Memory.Instance.CanEditItem())
            {
                LogError("Unable to load and apply layouts outside of Rotate Layout mode");
                return false;
            }

            if (!Config.ApplyLayout && !Memory.Instance.IsHousingMode())
            {
                LogError("Unable to load layouts outside of Layout mode");
                LogLayoutMode();
                return false;
            }

            return true;
        }

        private void SaveLayoutToFile()
        {
            if (!CheckModeForSave())
            {
                return;
            }

            try
            {
                Plugin.GetGameLayout();
                MakePlacePlugin.LayoutManager.ExportLayout();
            }
            catch (Exception e)
            {
                LogError($"Save Error: {e.Message}", e.StackTrace);
            }
        }

        private void LoadLayoutFromFile()
        {
            if (!CheckModeForLoad()) return;

            try
            {
                SaveLayoutManager.ImportLayout(Config.SaveLocation);
                Log(String.Format("Imported {0} items", Plugin.InteriorItemList.Count + Plugin.ExteriorItemList.Count));

                Plugin.MatchLayout();
                Config.ResetRecord();

                if (Config.ApplyLayout)
                {
                    Plugin.ApplyLayout();
                }
            }
            catch (Exception e)
            {
                LogError($"Load Error: {e.Message}", e.StackTrace);
            }
        }

        unsafe private void DrawGeneralSettings()
        {
            try
            {
                if (ImGui.Checkbox("Label Furniture", ref Config.DrawScreen)) Config.Save();
                if (Config.ShowTooltips && ImGui.IsItemHovered())
                    ImGui.SetTooltip("Show furniture names on the screen");

                ImGui.SameLine();
                ImGui.Dummy(new Vector2(10, 0));
                ImGui.SameLine();
                if (ImGui.Checkbox("##hideTooltipsOnOff", ref Config.ShowTooltips)) Config.Save();
                ImGui.SameLine();
                ImGui.TextUnformatted("Show Tooltips");

                ImGui.Dummy(new Vector2(0, 10));

                ImGui.Text("Layout");

                if (!Config.SaveLocation.IsNullOrEmpty())
                {
                    ImGui.Text($"Current file location: {Config.SaveLocation}");

                    if (ImGui.Button("Save"))
                    {
                        SaveLayoutToFile();
                    }
                    if (Config.ShowTooltips && ImGui.IsItemHovered()) ImGui.SetTooltip("Save layout to current file location");
                    ImGui.SameLine();
                }

                if (ImGui.Button("Save As"))
                {
                    if (CheckModeForSave())