﻿// <copyright file="WindowsXamlHost.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <author>Microsoft</author>

namespace Microsoft.Toolkit.Win32.UI.Interop.WPF
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows.Interop;

    /// <summary>
    /// WindowsXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
    /// </summary>
    partial class WindowsXamlHostBase : HwndHost
    {
        #region Fields

        /// <summary>
        /// UWP XAML Application instance and root UWP XamlMetadataProvider.  Custom implementation required to 
        /// probe at runtime for custom UWP XAML type information.  This must be created before 
        /// creating any DesktopWindowXamlSource instances if custom UWP XAML types are required.
        /// </summary>
        [ThreadStatic]
        private global::Windows.UI.Xaml.Application application;

        /// <summary>
        /// UWP XAML DesktopWindowXamlSource instance that hosts XAML content in a win32 application
        /// </summary>
        protected global::Windows.UI.Xaml.Hosting.DesktopWindowXamlSource desktopWindowXamlSource;

        /// <summary>
        /// A reference count on the UWP XAML framework is tied to WindowsXamlManager's 
        /// lifetime.  UWP XAML is spun up on the first WindowsXamlManager creation and 
        /// deinitialized when the last instance of WindowsXamlManager is destroyed.
        /// </summary>
        private global::Windows.UI.Xaml.Hosting.WindowsXamlManager windowsXamlManager;

        /// <summary>
        ///    Root UWP XAML content attached to WindowsXamlHost
        /// </summary>
        protected global::Windows.UI.Xaml.UIElement xamlRoot;

        #endregion

        #region Constructors and Initialization
        /// <summary>
        /// Initializes a new instance of the WindowsXamlHost class: default constructor is required for use in WPF markup.
        /// (When the default constructor is called, object properties have not been set. Put WPF logic in OnInitialized.)
        /// </summary>
        public WindowsXamlHostBase() : base()
        {
            // Create a custom UWP XAML Application object that implements reflection-based XAML metdata probing.
            // Instantiation of the application object must occur before creating the DesktopWindowXamlSource instance. 
            // DesktopWindowXamlSource will create a generic Application object unable to load custom UWP XAML metadata.
            if (this.application == null)
            {
                try
                {
                    // global::Windows.UI.Xaml.Application.Current may throw if DXamlCore has not been initialized.
                    // Treat the exception as an uninitialized global::Windows.UI.Xaml.Application condition.
                    this.application = global::Windows.UI.Xaml.Application.Current as XamlApplication;
                }
                catch
                {
                    this.application = new XamlApplication();
                }
            }

            // Create an instance of the WindowsXamlManager. This initializes and holds a 
            // reference on the UWP XAML DXamlCore and must be explicitly created before 
            // any UWP XAML types are programmatically created.  If WindowsXamlManager has 
            // not been created before creating DesktopWindowXamlSource, DesktopWindowXaml source
            // will create an instance of WindowsXamlManager internally.  (Creation is explicit
            // here to illustrate how to initialize UWP XAML before initializing the DesktopWindowXamlSource.) 
            windowsXamlManager = global::Windows.UI.Xaml.Hosting.WindowsXamlManager.InitializeForCurrentThread();

            // Create DesktopWindowXamlSource, host for UWP XAML content
            this.desktopWindowXamlSource = new global::Windows.UI.Xaml.Hosting.DesktopWindowXamlSource();

            // Hook OnTakeFocus event for Focus processing
            this.desktopWindowXamlSource.TakeFocusRequested += this.OnTakeFocusRequested;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the root UWP XAML element displayed in the WPF control instance.  This UWP XAML element is 
        /// the root element of the wrapped DesktopWindowXamlSource.
        /// </summary>
        public global::Windows.UI.Xaml.UIElement XamlRootInternal
        {
            get
            {
                return this.xamlRoot;
            }

            set
            {
                this.xamlRoot = value;
            }
        }

        /// <summary>
        /// Has this wrapper control instance been disposed?
        /// </summary>
        private bool IsDisposed { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates global::Windows.UI.Xaml.Application object, wrapped DesktopWindowXamlSource instance; creates and
        /// sets root UWP XAML element on DesktopWindowXamlSource.
        /// </summary>
        /// <param name="hwndParent">Parent window handle</param>
        /// <returns>Handle to XAML window</returns>
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            // 'EnableMouseInPointer' is called by the WindowsXamlManager during initialization. No need
            // to call it directly here. 

            // Create DesktopWindowXamlSource instance
            IDesktopWindowXamlSourceNative desktopWindowXamlSourceNative = this.desktopWindowXamlSource.GetInterop();

            // Associate the window where UWP XAML will display content
            desktopWindowXamlSourceNative.AttachToWindow(hwndParent.Handle);

            IntPtr windowHandle = desktopWindowXamlSourceNative.WindowHandle;

            // Overridden function must return window handle of new target window (DesktopWindowXamlSource's Window)
            return new HandleRef(this, windowHandle);
        }

        /// <summary>
        /// WPF framework request to destroy control window.  Cleans up the HwndIslandSite created by DesktopWindowXamlSource
        /// </summary>
        /// <param name="hwnd">Handle of window to be destroyed</param>
        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            this.Dispose(true);
        }

        /// <summary>
        /// WindowsXamlHost Dispose
        /// </summary>
        /// <param name="disposing">Is disposing?</param>
        protected override void Dispose(bool disposing)
        { 
            if (disposing && !this.IsDisposed)
            {
                this.IsDisposed = true;
                this.desktopWindowXamlSource.TakeFocusRequested -= this.OnTakeFocusRequested;
                this.xamlRoot = null;
                this.desktopWindowXamlSource.Dispose();
                this.desktopWindowXamlSource = null;
            }
        }

        #endregion
    }
}