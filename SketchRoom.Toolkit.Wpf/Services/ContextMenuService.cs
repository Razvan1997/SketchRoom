using SketchRoom.Models.Enums;
using SketchRoom.Toolkit.Wpf.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WhiteBoard.Core.Services.Interfaces;

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
                case ShapeContextType.WhiteBoardArea:
                    AddWhiteBoardItems(contextMenu, owner);
                    break;
                case ShapeContextType.TextArea:
                    AddTextAreaItems(contextMenu, owner);
                    break;
                case ShapeContextType.ConnectorDouble:
                    AddConnectorDoubleItems(contextMenu, owner);
                    break;
                case ShapeContextType.ConnectorSimpleLabel:
                    AddConnectorDoubleItems(contextMenu, owner);
                    break;
                case ShapeContextType.DescriptionShapeConnector:
                    AddConnectorDoubleItems(contextMenu, owner);
                    break;
            }

            return contextMenu;
        }

        private void AddConnectorDoubleItems(ContextMenu menu, object owner)
        {
            if (owner is IInteractiveShape shape)
            {
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

                var changeMarginsColorMenuItem = new MenuItem
                {
                    Header = "Change Line Color"
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
            }
        }

        private void AddTextAreaItems(ContextMenu menu, object owner)
        {
            var _prefernces = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

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
                        shape.RequestChangeBackgroundColor(picker.SelectedColor);
                    }
                };
                menu.Items.Add(changeBackgroundColorMenuItem);

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
                        shape.RequestChangeForegroundColor(picker.SelectedColor);
                    }
                };
                menu.Items.Add(changeColorText);

                var changeFontSize = new MenuItem
                {
                    Header = "Apply Current Font Size"
                };

                changeFontSize.Click += (s, e) =>
                {
                    shape.RequestChangeCurrentFontSize(_prefernces.FontSize);
                };
                menu.Items.Add(changeFontSize);
            }
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
            if (owner is IShapeAddedXaml shape && shape.TableShape is ITableShapeRender table)
            {
                var addRowAbove = new MenuItem { Header = "Add Row Above" };
                addRowAbove.Click += (_, _) =>
                {
                    int? row = table.GetLastRowClicked();
                    int? col = table.GetLastColumnClicked();

                    if (row.HasValue)
                        table.AddRowAbove(row.Value);
                };
                menu.Items.Add(addRowAbove);

                var addRowBelow = new MenuItem { Header = "Add Row Below" };
                addRowBelow.Click += (_, _) =>
                {
                    int? row = table.GetLastRowClicked();
                    int? col = table.GetLastColumnClicked();
                    if (row.HasValue)
                        table.AddRowBelow(row.Value);
                };
                menu.Items.Add(addRowBelow);

                var addColumnLeft = new MenuItem { Header = "Add Column Left" };
                addColumnLeft.Click += (_, _) =>
                {
                    int? row = table.GetLastRowClicked();
                    int? col = table.GetLastColumnClicked();
                    if (col.HasValue)
                        table.AddColumnLeft(col.Value);
                };
                menu.Items.Add(addColumnLeft);

                var addColumnRight = new MenuItem { Header = "Add Column Right" };
                addColumnRight.Click += (_, _) =>
                {
                    int? row = table.GetLastRowClicked();
                    int? col = table.GetLastColumnClicked();
                    if (col.HasValue)
                        table.AddColumnRight(col.Value);
                };
                menu.Items.Add(addColumnRight);

                var deleteRow = new MenuItem { Header = "Delete Row" };
                deleteRow.Click += (_, _) =>
                {
                    int? row = table.GetLastRowClicked();
                    int? col = table.GetLastColumnClicked();
                    if (row.HasValue)
                        table.DeleteRow(row.Value);
                };
                menu.Items.Add(deleteRow);

                var deleteColumn = new MenuItem { Header = "Delete Column" };
                deleteColumn.Click += (_, _) =>
                {
                    int? row = table.GetLastRowClicked();
                    int? col = table.GetLastColumnClicked();
                    if (col.HasValue)
                        table.DeleteColumn(col.Value);
                };
                menu.Items.Add(deleteColumn);

                var changeHeaderColor = new MenuItem { Header = "Change Header Background" };
                changeHeaderColor.Click += (_, _) =>
                {
                    var picker = new ColorPickerWindow(Brushes.White)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (picker.ShowDialog() == true)
                    {
                        table.ChangeHeaderBackground(picker.SelectedColor);
                    }
                };
                menu.Items.Add(changeHeaderColor);

                var changeBorderColor = new MenuItem { Header = "Change Place Color" };
                changeBorderColor.Click += (_, _) =>
                {
                    var picker = new ColorPickerWindow(Brushes.White)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (picker.ShowDialog() == true)
                    {
                        table.ChangeBorderColor(picker.SelectedColor);
                    }
                };
                menu.Items.Add(changeBorderColor);
            }
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

        private void AddWhiteBoardItems(ContextMenu menu, object owner)
        {
            if (owner is WhiteBoardControl whiteBoard)
            {
                //var copyItem = new MenuItem
                //{
                //    Header = "Copy"
                //};
                //copyItem.Click += (s, e) =>
                //{
                //    // Copiază elementele selectate (logica o implementezi în WhiteBoardControl)
                //    whiteBoard.CopySelectedElements();
                //};
                //menu.Items.Add(copyItem);

                //var pasteItem = new MenuItem
                //{
                //    Header = "Paste"
                //};
                //pasteItem.Click += (s, e) =>
                //{
                //    // Lipește elementele copiate
                //    whiteBoard.PasteElements();
                //};
                //menu.Items.Add(pasteItem);

                //var deleteItem = new MenuItem
                //{
                //    Header = "Delete"
                //};
                //deleteItem.Click += (s, e) =>
                //{
                //    // Șterge selecția curentă
                //    whiteBoard.DeleteSelectedElements();
                //};
                //menu.Items.Add(deleteItem);

                var addImageItem = new MenuItem
                {
                    Header = "Add Image"
                };
                addImageItem.Click += (s, e) =>
                {
                    whiteBoard.AddImageAtPosition();
                };
                menu.Items.Add(addImageItem);
            }
        }
    }
}
