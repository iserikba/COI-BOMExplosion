using System;
using Mafi;
using Mafi.Unity.InputControl;
using Mafi.Unity.Ui.Hud;
using Mafi.Unity.UiStatic.Toolbar;
using UnityEngine;

namespace ProductionCalculator.Ui
{
	// Token: 0x02000007 RID: 7
	[GlobalDependency((Mafi.RegistrationMode)3, false, false)]
	public sealed class ProductionCalculatorUiController : WindowController<ProductionCalculatorWindow>, IToolbarItemController, IUnityInputController
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600001B RID: 27 RVA: 0x0000288F File Offset: 0x00000A8F
		public bool IsVisible
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600001C RID: 28 RVA: 0x00002892 File Offset: 0x00000A92
		public bool DeactivateShortcutsIfNotVisible
		{
			get
			{
				return true;
			}
		}


		public event Action<IToolbarItemController> VisibilityChanged;

		// Token: 0x0600001F RID: 31 RVA: 0x00002908 File Offset: 0x00000B08
		public ProductionCalculatorUiController(ControllerContext controllerContext, ToolbarHud toolbar) : base(controllerContext, null)
		{
			toolbar.AddMainMenuButton(Tr.WindowTitle, this, "Assets/Unity/UserInterface/Toolbar/Stats.svg", 905f, 
				(ShortcutsManager sm) => KeyBindings.FromKey((KbCategory)2,(ShortcutMode) 1,(KeyCode) 291)
				);
			Log.Info("ProductionCalculator: toolbar button registered (F10)");
		}

		// Token: 0x06000020 RID: 32 RVA: 0x0000296A File Offset: 0x00000B6A
		protected override void OnActivate()
		{
			base.Window.RefreshPreview();
		}

		// Token: 0x0400002E RID: 46
		private const string ToolbarIconPath = "Assets/Unity/UserInterface/Toolbar/Stats.svg";

		// Token: 0x0400002F RID: 47
		private const float ToolbarOrder = 905f;
	}
}
