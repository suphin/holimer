//using ProjNet.CoordinateSystems.Transformations;
//using ProjNet.CoordinateSystems;
//using System.Diagnostics;

//namespace Ekomers.Web.Helpers
//{
//    public static class CoordinateTransformer
//    {
//        public static (double, double) Transform(double x, double y)
//        {
//            // Debugging: Log input coordinates
//            Debug.WriteLine($"Input Coordinates: x = {x}, y = {y}");

//            // Define your source coordinate system: UTM Zone 36N
//            var source = ProjectedCoordinateSystem.WGS84_UTM(36, true);

//            // Define the target coordinate system (WGS84)
//            var target = GeographicCoordinateSystem.WGS84;

//            // Create a coordinate transformation
//            var transformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(source, target);

//            // Transform the coordinates
//            double[] point = transformation.MathTransform.Transform(new double[] { x, y });

//            // Debugging: Log output coordinates
//            Debug.WriteLine($"Output Coordinates: lat = {point[1]}, lng = {point[0]}");

//            // Return the transformed coordinates
//            return (point[1], point[0]); // Note the order: lat, lng


//            //// Define your source coordinate system, e.g., UTM Zone 33N
//            //var source = ProjectedCoordinateSystem.WGS84_UTM(36, true);

//            //// Define the target coordinate system (WGS84)
//            //var target = GeographicCoordinateSystem.WGS84;

//            //// Create a coordinate transformation
//            //var transformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(source, target);

//            //// Transform the coordinates
//            //double[] point = transformation.MathTransform.Transform(new double[] { x, y });

//            //// Return the transformed coordinates
//            //return (point[1], point[0]); // Note the order: lat, lng
//        }
//    }
//}
