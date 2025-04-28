using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Media;
using SketchRoom.Toolkit.Wpf.Controls;

namespace SketchRoom.Toolkit.Wpf.Services
{
    public class ContextMenuService : IContextMenuService
    {
        public ContextMenu CreateContextMenu(ShapeContextType contextType, object owner)
        {
            var contextMenu = new ContextMenu
            {
                Style = (Style)Application.Current.FindResource("DarkContextMenuStyle")
            };

            switch (contextType)
            {
                case ShapeContextType.GenericShape:
                    AddGenericShapeItems(contextMenu, owner);
                    break;

                case ShapeContextType.TableShape:
                    AddTableShapeItems(contextMenu, owner);
                    break;

                case ShapeContextType.EntityShape:
                    AddEntityShapeItems(contextMenu, owner);
                    break;
                case ShapeContextType.SimpleLinesContainer:
                    AddSimpleLinesItems(contextMenu, owner);
                    break;
                case ShapeContextType.BorderTextBoxShape:
                    AddBorderTextShapeItems(contextMenu, owner);
                    break;
            }

            return contextMenu;
        }

        private void AddGenericShapeItems(ContextMenu menu, object owner)
        {
            if (owner is IInteractiveShape shape)
            {
                var addTextMenuItem = new MenuItem
                {
                    Header = "Add Text"
                };

                addTextMenuItem.Click += (s, e) =>
                {
                    shape.AddTextToCenter();
                };
                menu.Items.Add(addTextMenuItem);

                var changeBackgroundColorMenuItem = new MenuItem
                {
                    Header = "Change Background Color"
                };

                changeBackgroundColorMenuItem.Click += (s, e) =>
                {
                    var picker = new ColorPickerWindow(Brushes.White)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (picker.ShowDialog() == true)
                    {
                        shape.RequestChangeBackgroundColor(picker.SelectedColor); // 🔥 schimbat aici
                    }
                };
                menu.Items.Add(changeBackgroundColorMenuItem);

                var changeMarginsColorMenuItem = new MenuItem
                {
                    Header = "Change Margins Color"
                };

                changeMarginsColorMenuItem.Click += (s, e) =>
                {
                    var picker = new ColorPickerWindow(Brushes.White)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (picker.ShowDialog() == true)
                    {
                        shape.RequestChangeStrokeColor(picker.SelectedColor); // 🔥 schimbat aici
                    }
                };
                menu.Items.Add(changeMarginsColorMenuItem);

                var changeColorText = new MenuItem
                {
                    Header = "Change Foreground Color"
                };

                changeColorText.Click += (s, e) =>
                {
                    var picker = new ColorPickerWindow(Brushes.White)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (picker.ShowDialog() == true)
                    {
                        shape.RequestChangeForegroundColor(picker.SelectedColor); // 🔥 schimbat aici
                    }
                };
                menu.Items.Add(changeColorText);
            }
        }

        private void AddSimpleLinesItems(ContextMenu menu, object owner)
        {
            if (owner is ISimpleLinesContainer container)
            {
                var addLineMenuItem = new MenuItem
                {
                    Header = "Add Line"
                };

                addLineMenuItem.Click += (s, e) =>
                {
                    container.AddLine(); // adaugă linie simplă
                };

                var addTextMenuItem = new MenuItem
                {
                    Header = "Add Text"
                };

                addTextMenuItem.Click += (s, e) =>
                {
                    container.AddTextToCenter(); // adaugă text
                };

                menu.Items.Add(addLineMenuItem);
                menu.Items.Add(addTextMenuItem);
            }
        }

        private void AddBorderTextShapeItems(ContextMenu menu, object owner)
        {
            if (owner is IInteractiveShape shape)
            {
                var changeBackgroundColorMenuItem = new MenuItem
                {
                    Header = "Change Background Color"
                };

                changeBackgroundColorMenuItem.Click += (s, e) =>
                {
                    var picker = new ColorPickerWindow(Brushes.White)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (picker.ShowDialog() == true)
                    {
                        shape.RequestChangeBackgroundColor(picker.SelectedColor); // 🔥 schimbat aici
                    }
                };
                menu.Items.Add(changeBackgroundColorMenuItem);

                var changeMarginsColorMenuItem = new MenuItem
                {
                    Header = "Change Margins Color"
                };

                changeMarginsColorMenuItem.Click += (s, e) =>
                {
                    var picker = new ColorPickerWindow(Brushes.White)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (picker.ShowDialog() == true)
                    {
                        shape.RequestChangeStrokeColor(picker.SelectedColor); // 🔥 schimbat aici
                    }
                };
                menu.Items.Add(changeMarginsColorMenuItem);

                var changeColorText = new MenuItem
                {
                    Header = "Change Foreground Color"
                };

                changeColorText.Click += (s, e) =>
                {
                    var picker = new ColorPickerWindow(Brushes.White)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (picker.ShowDialog() == true)
                    {
                        shape.RequestChangeForegroundColor(picker.SelectedColor); // 🔥 schimbat aici
                    }
                };
                menu.Items.Add(changeColorText);
            }
        }

        private void AddTableShapeItems(ContextMenu menu, object owner)
        {
            // aici pui iteme specifice pentru table
        }

        private void AddEntityShapeItems(ContextMenu menu, object owner)
        {
            if (owner is IShapeAddedXaml shape)
            {
                var addRowItem = new MenuItem
                {
                    Header = "Add Row"
                };

                addRowItem.Click += (s, e) =>
                {
                    if (shape.Renderer is IShapeEntityRenderer entityRenderer)
                    {
                        entityRenderer.AddRow();
                    }
                };
                menu.Items.Add(addRowItem);

                var removeRowItem = new MenuItem
                {
                    Header = "Remove Row"
                };

                removeRowItem.Click += (s, e) =>
                {
                    if (shape.Renderer is IShapeEntityRenderer entityRenderer)
                    {
                        // Trebuie să știi pe ce rând ai dat click dreapta
                        if (entityRenderer.LastRightClickedRow != null)
                        {
                            entityRenderer.RemoveRowAt(entityRenderer.LastRightClickedRow);
                        }
                    }
                };
                menu.Items.Add(removeRowItem);

                var changeHeaderBackground = new MenuItem
                {
                    Header = "Change Header Background"
                };

                changeHeaderBackground.Click += (s, e) =>
                {
                    if (shape.Renderer is IShapeEntityRenderer entityRenderer)
                    {
                        var picker = new ColorPickerWindow(Brushes.White)
                        {
                            Owner = Application.Current.MainWindow
                        };

                        if (picker.ShowDialog() == true)
                        {
                            entityRenderer.ChangeHeaderBackground(picker.SelectedColor);
                        }
                    }
                };
                menu.Items.Add(changeHeaderBackground);
            }
        }
    }
}
