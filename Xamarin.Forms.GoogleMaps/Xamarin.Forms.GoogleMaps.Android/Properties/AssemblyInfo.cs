using Android;
using Android.App;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using Xamarin.Forms.GoogleMaps.Android;
using Xamarin.Forms.Internals;
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("Xamarin.Forms.GoogleMaps.Android")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]

// Add some common permissions, these can be removed if not needed

[assembly: UsesPermission(Manifest.Permission.Internet)]
[assembly: InternalsVisibleTo("Xamarin.Forms.GoogleMaps.Clustering.Android")]
[assembly: ExportRenderer(typeof(Map), typeof(MapRenderer))]
[assembly: Preserve]
