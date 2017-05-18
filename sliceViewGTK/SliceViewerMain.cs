﻿using System;
using System.Collections.Generic;
using System.IO;

using Gtk;
using GLib;
using SkiaSharp;
using g3;
using gs;

namespace SliceViewer
{


	class MainClass
	{


		public static void Main(string[] args)
		{
			ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs) {
				Console.WriteLine(expArgs.ExceptionObject.ToString());
				expArgs.ExitApplication = true;
			};

			Gtk.Application.Init();

			var window = new Window("SliceViewer");
			window.SetDefaultSize(900, 600);
			window.SetPosition(WindowPosition.Center);
			window.DeleteEvent += delegate {
				Gtk.Application.Quit();
			};



			//string sPath = "../../../sample_files/disc_single_layer.gcode";
			//string sPath = "../../../sample_files/disc_0p6mm.gcode";
			//string sPath = "../../../sample_files/square_linearfill.gcode";
			string sPath = "../../../sample_files/thin_hex_test_part.gcode";


#if true
			//GCodeFile genGCode = MakerbotTests.SimpleFillTest();
			//GCodeFile genGCode = MakerbotTests.SimpleShellsTest();

			GeneralPolygon2d poly = GetPolygonFromMesh("../../../sample_files/bunny_open.obj");
			//GCodeFile genGCode = MakerbotTests.ShellsPolygonTest(poly);
			//GCodeFile genGCode = MakerbotTests.StackedPolygonTest(poly, 2);
			GCodeFile genGCode = MakerbotTests.StackedScaledPolygonTest(poly, 20, 0.5);

			string sWritePath = "../../../sample_output/generated.gcode";
			StandardGCodeWriter writer = new StandardGCodeWriter();
			using ( StreamWriter w = new StreamWriter(sWritePath) ) {
				writer.WriteFile(genGCode, w);
			}
			sPath = sWritePath;
#endif


			GenericGCodeParser parser = new GenericGCodeParser();
			GCodeFile gcode;
			using (FileStream fs = new FileStream(sPath, FileMode.Open, FileAccess.Read)) {
				using (TextReader reader = new StreamReader(fs) ) {
					gcode = parser.Parse(reader);
				}
			}


			// write back out gcode we loaded
			//StandardGCodeWriter writer = new StandardGCodeWriter();
			//using ( StreamWriter w = new StreamWriter("../../../sample_output/writeback.gcode") ) {
			//	writer.WriteFile(gcode, w);
			//}

			GCodeToLayerPaths converter = new GCodeToLayerPaths();
			MakerbotInterpreter interpreter = new MakerbotInterpreter();
			interpreter.AddListener(converter);

			InterpretArgs interpArgs = new InterpretArgs();
			interpreter.Interpret(gcode, interpArgs);

			MakerbotSettings settings = new MakerbotSettings();
			CalculateExtrusion calc = new CalculateExtrusion(converter.Paths, settings);
			calc.TestCalculation();

			PathSet Layer = converter.Paths;

			//PathSet Layer = new PathSet();
			//LinearPath2 path = new LinearPath2();
			//path.AppendVertex(new Vector2d(100, 100));
			//path.AppendVertex(new Vector2d(400, 100));
			//path.AppendVertex(new Vector2d(400, 400));
			//path.AppendVertex(new Vector2d(100, 400));
			//path.AppendVertex(new Vector2d(100, 105));
			//Layer.Append(path);


            var darea = new SliceViewCanvas();
			darea.Paths = Layer;
            window.Add(darea);

            window.ShowAll();

            Gtk.Application.Run();
        }

		void OnException(object o, UnhandledExceptionArgs args)
		{

		}




		static GeneralPolygon2d GetPolygonFromMesh(string sPath) {
			DMesh3 mesh = StandardMeshReader.ReadMesh(sPath);
			MeshBoundaryLoops loops = new MeshBoundaryLoops(mesh);

			PlanarComplex complex = new PlanarComplex();

			foreach (var loop in loops ) {
				Polygon2d poly = new Polygon2d();
				DCurve3 curve = MeshUtil.ExtractLoopV(mesh, loop.Vertices);
				foreach (Vector3d v in curve.Vertices)
					poly.AppendVertex(v.xy);
				complex.AddPolygon(poly);
			}

			PlanarComplex.SolidRegionInfo solids = complex.FindSolidRegions(0.0, false);
			return solids.Polygons[0];
		}



	}
}
