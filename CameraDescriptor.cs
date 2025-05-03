using System.Numerics;
using Silk.NET.Maths;

namespace Lab4
{
    internal class CameraDescriptor
    {
        private Vector3D<float> offset = new Vector3D<float>(0, 0, 0);
        public double DistanceToOrigin { get; private set; } = 1;

        public double AngleToZYPlane { get; private set; } = 0;

        public double AngleToZXPlane { get; private set; } = 0;

        const double DistanceScaleFactor = 1.1;

        const float AngleChangeStepSize = (float)Math.PI / 180 * 5;

        public void setOffset(int key)
        {
            switch (key)
            {
                case 0:
                    offset.Z -= 0.5f;
                    break;
                case 1:
                    offset.X -= 0.5f;
                    break;
                case 2:
                    offset.Z += 0.5f;
                    break;
                case 3:
                    offset.X += 0.5f;
                    break;
                case 4:
                    offset.Y += 0.5f;
                    break;
                case 5:
                    offset.Y -= 0.5f;
                    break;
            }
        }

        /// <summary>
        /// Gets the position of the camera.1
        /// </summary>
        public Vector3D<float> Position
        {
            get
            {
                return GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane) + offset;
            }
        }

        /// <summary>
        /// Gets the up vector of the camera.
        /// </summary>
        public Vector3D<float> UpVector
        {
            get
            {
                return Vector3D.Normalize(GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane + Math.PI / 2));
            }
        }

        /// <summary>
        /// Gets the target point of the camera view.
        /// </summary>
        public Vector3D<float> Target
        {
            get
            {
                return Vector3D<float>.Zero + offset;
            }
        }

        public void IncreaseZXAngle()
        {
            AngleToZXPlane += AngleChangeStepSize;
        }

        public void DecreaseZXAngle()
        {
            AngleToZXPlane -= AngleChangeStepSize;
        }

        public void IncreaseZYAngle()
        {
            AngleToZYPlane += AngleChangeStepSize;

        }

        public void DecreaseZYAngle()
        {
            AngleToZYPlane -= AngleChangeStepSize;
        }

        public void IncreaseDistance()
        {
            DistanceToOrigin = DistanceToOrigin * DistanceScaleFactor;
        }

        public void DecreaseDistance()
        {
            DistanceToOrigin = DistanceToOrigin / DistanceScaleFactor;
        }

        private static Vector3D<float> GetPointFromAngles(double distanceToOrigin, double angleToMinZYPlane, double angleToMinZXPlane)
        {
            var x = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Sin(angleToMinZYPlane);
            var z = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Cos(angleToMinZYPlane);
            var y = distanceToOrigin * Math.Sin(angleToMinZXPlane);

            return new Vector3D<float>((float)x, (float)y, (float)z);
        }
    }
}