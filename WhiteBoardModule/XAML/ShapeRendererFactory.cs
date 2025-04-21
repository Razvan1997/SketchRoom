using SketchRoom.Models.Enums;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoardModule.XAML.Shapes.Connectors;
using WhiteBoardModule.XAML.Shapes.Containers;
using WhiteBoardModule.XAML.Shapes.Entity;
using WhiteBoardModule.XAML.Shapes.General;
using WhiteBoardModule.XAML.Shapes.Nodes;
using WhiteBoardModule.XAML.Shapes.States;
using WhiteBoardModule.XAML.Shapes.Tables;

namespace WhiteBoardModule.XAML
{
    public class ShapeRendererFactory : IShapeRendererFactory
    {
        public IShapeRenderer CreateRenderer(ShapeType type, bool withBindings = false)
        {
            return type switch
            {
                ShapeType.Ellipse => new EllipseShapeRenderer(withBindings),
                ShapeType.Rectangle => new RectangleShapeRenderer(withBindings),
                ShapeType.Triangle => new TriangleShapeRenderer(withBindings),
                ShapeType.TableShape => new TableShapeRenderer(),
                ShapeType.ShapeText => new TextShapeRenderer(withBindings),
                ShapeType.ConnectorShapeLabel => new ConnectorLabelShapeRenderer(withBindings),
                ShapeType.ConnectorDoubleShapeLabel => new ConnectorDoubleLabelShapeRenderer(withBindings),
                ShapeType.ContainerHorizontalShape => new HorizontalContainerShapeRenderer(withBindings),
                ShapeType.ContainerHorizontalPoolLineOneShape => new HorizontalPoolLaneOneRenderer(withBindings),
                ShapeType.ContainerHorizontalPoolLineTwoShape => new HorizontalPoolLineTwoRender(withBindings),
                ShapeType.EntityShape => new EntityShapeRenderer(withBindings),
                ShapeType.ListContainerShape => new ListContainerRenderer(withBindings),
                ShapeType.BraceToRightShape => new BraceToRightShapeRender(withBindings),
                ShapeType.StraightBraceRightShape => new StraightBraceRightShapeRenderer(withBindings),
                ShapeType.ObjectTypeShape => new ObjectTypeShapeRenderer(withBindings),
                ShapeType.UmlClassTypeShape => new UmlClassShapeRenderer(withBindings),
                ShapeType.StateMachineShape => new StateMachineShapeRenderer(withBindings),
                ShapeType.AdvancedTreeShapeRenderer => new AdvancedTreeShapeRenderer(withBindings),
                ShapeType.BorderTextBox => new TextBoxBorderShapeRenderer(withBindings),
                _ => throw new NotImplementedException($"Renderer for {type} not implemented.")
            };
        }

        public UIElement CreateRenderPreview(ShapeType type)
        {
            var renderer = CreateRenderer(type, withBindings: false);
            return renderer.CreatePreview();
        }
    }
}
