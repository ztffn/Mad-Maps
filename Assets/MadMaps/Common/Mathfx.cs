using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MadMaps.Common
{
    public static class Mathfx
    {
        public static float Frac(this float f)
        {
            return f - (float)Math.Truncate(f);
        }

        public static bool IntersectsBounds(this Plane plane, Bounds b)
        {
            // Get all points of the bounds
            var p1 = b.center + new Vector3(b.extents.x, b.extents.y, b.extents.z);
            var p2 = b.center + new Vector3(b.extents.x, -b.extents.y, b.extents.z);
            var p3 = b.center + new Vector3(b.extents.x, b.extents.y, -b.extents.z);
            var p4 = b.center + new Vector3(b.extents.x, -b.extents.y, -b.extents.z);
            var p5 = b.center + new Vector3(-b.extents.x, b.extents.y, b.extents.z);
            var p6 = b.center + new Vector3(-b.extents.x, -b.extents.y, b.extents.z);
            var p7 = b.center + new Vector3(-b.extents.x, b.extents.y, -b.extents.z);
            var p8 = b.center + new Vector3(-b.extents.x, -b.extents.y, -b.extents.z);

            var b1 = plane.GetSide(p1);
            var b2 = plane.GetSide(p2);
            var b3 = plane.GetSide(p3);
            var b4 = plane.GetSide(p4);
            var b5 = plane.GetSide(p5);
            var b6 = plane.GetSide(p6);
            var b7 = plane.GetSide(p7);
            var b8 = plane.GetSide(p8);

            // if all true, OR all false, we aren't intersecting the bounds
            // results are mixed, we are intersecting

            var allTrue = (b1 && b2 && b3 && b4 && b5 && b6 && b7 && b8);
            var allFalse = (!b1 && !b2 && !b3 && !b4 && !b5 && !b6 && !b7 && !b8);
            return !(allFalse || allTrue);
        }

        public static float Hermite(float start, float end, float value)
        {
            return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
        }

        public static float Sinerp(float start, float end, float value)
        {
            return Mathf.Lerp(start, end, Mathf.Sin(value * Mathf.PI * 0.5f));
        }

        public static float Coserp(float start, float end, float value)
        {
            return Mathf.Lerp(start, end, 1.0f - Mathf.Cos(value * Mathf.PI * 0.5f));
        }

        public static float Clamp360(float value)
        {
            return (value % 360 + 360) % 360;
        }

        public static float Berp(float start, float end, float value)
        {
            value = Mathf.Clamp01(value);
            value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
            return start + (end - start) * value;
        }

        public static float EaseOutCurve(float value)
        {
            return 1 - Mathf.Pow(value - 1, 2);
        }

        public static float SmoothStep(float x, float min, float max)
        {
            x = Mathf.Clamp(x, min, max);
            float v1 = (x - min) / (max - min);
            float v2 = (x - min) / (max - min);
            return -2 * v1 * v1 * v1 + 3 * v2 * v2;
        }

        public static float Lerp(float start, float end, float value)
        {
            return ((1.0f - value) * start) + (value * end);
        }

        public static Vector3 Clamp(Vector3 vector, float min, float max)
        {
            return new Vector3(Mathf.Clamp(vector.x, min, max), Mathf.Clamp(vector.y, min, max), Mathf.Clamp(vector.z, min, max));
        }

        public static Vector3 NearestPoint(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
        {
            Vector3 lineDirection = Vector3.Normalize(lineEnd - lineStart);
            float closestPoint = Vector3.Dot((point - lineStart), lineDirection) / Vector3.Dot(lineDirection, lineDirection);
            return lineStart + (closestPoint * lineDirection);
        }

        public static Vector3 NearestPointDirection(Vector3 lineStart, Vector3 lineDirection, Vector3 point)
        {
            float closestPoint = Vector3.Dot((point - lineStart), lineDirection) / Vector3.Dot(lineDirection, lineDirection);
            return lineStart + (closestPoint * lineDirection);
        }

        public static Vector3 NearestPointStrict(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
        {
            Vector3 fullDirection = lineEnd - lineStart;
            Vector3 lineDirection = Vector3.Normalize(fullDirection);
            float closestPoint = Vector3.Dot((point - lineStart), lineDirection) / Vector3.Dot(lineDirection, lineDirection);
            return lineStart + (Mathf.Clamp(closestPoint, 0.0f, Vector3.Magnitude(fullDirection)) * lineDirection);
        }
        public static float Bounce(float x)
        {
            return Mathf.Abs(Mathf.Sin(6.28f * (x + 1f) * (x + 1f)) * (1f - x));
        }

        // test for value that is near specified float (due to floating point inprecision)
        // all thanks to Opless for this!
        public static bool Approx(float val, float about, float range)
        {
            return ((Mathf.Abs(val - about) < range));
        }

        public static bool Approx(double val, double about, double range)
        {
            var abs = val - about;
            if (abs < 0) abs *= -1;

            return ((abs < range));
        }

        // test if a Vector3 is close to another Vector3 (due to floating point inprecision)
        // compares the square of the distance to the square of the range as this 
        // avoids calculating a square root which is much slower than squaring the range
        public static bool Approx(Vector3 val, Vector3 about, float range)
        {
            return ((val - about).sqrMagnitude < range * range);
        }

        /*
          * CLerp - Circular Lerp - is like lerp but handles the wraparound from 0 to 360.
          * This is useful when interpolating eulerAngles and the object
          * crosses the 0/360 boundary.  The standard Lerp function causes the object
          * to rotate in the wrong direction and looks stupid. Clerp fixes that.
          */
        public static float Clerp(float start, float end, float value)
        {
            float min = 0.0f;
            float max = 360.0f;
            float half = Mathf.Abs((max - min) / 2.0f);//half the distance between min and max
            float retval = 0.0f;
            float diff = 0.0f;

            if ((end - start) < -half)
            {
                diff = ((max - start) + end) * value;
                retval = start + diff;
            }
            else if ((end - start) > half)
            {
                diff = -((max - end) + start) * value;
                retval = start + diff;
            }
            else retval = start + (end - start) * value;

            // Debug.Log("Start: "  + start + "   End: " + end + "  Value: " + value + "  Half: " + half + "  Diff: " + diff + "  Retval: " + retval);
            return retval;
        }

        public static Vector3 QuakeClip(Vector3 source, Vector3 normal, float overbounce = 1.011f)
        {

            float backoff;
            backoff = Vector3.Dot(source, normal);

            if (Mathf.Approximately(backoff, 0))
            {
                return source;
            }

            if (backoff < 0)
            {
                backoff *= overbounce;
            }
            else
            {
                backoff /= overbounce;
            }

            var change = normal * backoff;

            return source - change;
        }

        public static Vector3 QuakeClipInverse(Vector3 source, Vector3 normal, bool forwardOnly = false, float overbounce = 1.011f)
        {
            float backoff = Vector3.Dot(source, normal);

            if (backoff < 0)
            {
                if (!forwardOnly)
                {
                    backoff *= overbounce;
                }
                else
                {
                    return Vector3.zero;
                }
            }
            else
            {
                backoff /= overbounce;
            }

            var change = normal * backoff;

            return change;
        }

        public static bool VectorApprox(Vector3 a, Vector3 b)
        {
            return (Mathf.Approximately(a.x, b.x)
                    && Mathf.Approximately(a.y, b.y)
                    && Mathf.Approximately(a.z, b.z));

        }

        public static Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P)
        {
            Vector2 AP = P - A;       //Vector from A to P   
            Vector2 AB = B - A;       //Vector from A to B  

            float magnitudeAB = AB.sqrMagnitude;     //Magnitude of AB vector (it's length squared)     
            float ABAPproduct = Vector2.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b     
            float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

            if (distance < 0)     //Check if P projection is over vectorAB     
            {
                return A;

            }
            else if (distance > 1)
            {
                return B;
            }
            else
            {
                return A + AB * distance;
            }
        }

        public static Vector3 GetClosestPointOnLineSegment3D(Vector3 A, Vector3 B, Vector3 P)
        {
            Vector3 AP = P - A;       //Vector from A to P   
            Vector3 AB = B - A;       //Vector from A to B  

            float magnitudeAB = AB.sqrMagnitude;     //Magnitude of AB vector (it's length squared)     
            float ABAPproduct = Vector3.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b     
            float distance = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point  

            if (distance < 0)     //Check if P projection is over vectorAB     
            {
                return A;

            }
            else if (distance > 1)
            {
                return B;
            }
            else
            {
                return A + AB * distance;
            }
        }

        public static bool LineIntersectsCircle(Vector2 circlePos, float circleRadius, Vector2 line1, Vector2 line2)
        {
            var dx = line1.x - line2.x;
            var dz = line1.y - line2.y;

            var A = dx * dx + dz * dz;
            var B = 2 * (dx * (line1.x - circlePos.x) + dz * (line1.y - circlePos.y));
            var C = (line1.x - circlePos.x) * (line1.x - circlePos.x) + (line1.y - circlePos.y) * (line1.y - circlePos.y) -
                    circleRadius * circleRadius;

            var det = B * B - 4 * A * C;

            if (A <= 0.000001f || det < 0)
            {
                return false;
            }
            return true;
        }


        public static Vector3 CalculateInterceptCourse(Vector3 aTargetPos, Vector3 aTargetSpeed, Vector3 aInterceptorPos, float aInterceptorSpeed, float maxLead = 9999f)
        {
            Vector3 targetDir = aTargetPos - aInterceptorPos;

            if (aTargetSpeed.sqrMagnitude < 0.0001f)
            {
                //return aTargetPos;
            }
            float iSpeed2 = aInterceptorSpeed * aInterceptorSpeed;
            float tSpeed2 = aTargetSpeed.sqrMagnitude;
            float fDot1 = Vector3.Dot(targetDir, aTargetSpeed);
            float targetDist2 = targetDir.sqrMagnitude;
            float d = (fDot1 * fDot1) - targetDist2 * (tSpeed2 - iSpeed2);
            if (d < 0.1f)  // negative == no possible course because the interceptor isn't fast enough
            {
                return aTargetPos + aTargetSpeed.normalized * maxLead;
            }
            float sqrt = Mathf.Sqrt(d);
            float S1 = (-fDot1 - sqrt) / targetDist2;
            float S2 = (-fDot1 + sqrt) / targetDist2;

            Vector3 result;

            if (S1 < 0.0001f)
            {
                if (S2 < 0.0001f)
                {
                    return aTargetPos + aTargetSpeed.normalized * maxLead;
                }
                else
                    result = (S2) * targetDir + aTargetSpeed;
            }
            else if (S2 < 0.0001f)
                result = (S1) * targetDir + aTargetSpeed;
            else if (S1 < S2)
                result = (S2) * targetDir + aTargetSpeed;
            else
                result = (S1) * targetDir + aTargetSpeed;

            result.Normalize();
            return FindClosestPointOfApproach(aTargetPos, aTargetSpeed, aInterceptorPos, result * aInterceptorSpeed) * aTargetSpeed;
            //var interceptionDistance = result.magnitude;
            //length = Mathf.Min(interceptionDistance, maxLead);
            //return (result / interceptionDistance) * length;


        }

        public static float FindClosestPointOfApproach(Vector3 aPos1, Vector3 aSpeed1, Vector3 aPos2, Vector3 aSpeed2)
        {
            Vector3 PVec = aPos1 - aPos2;
            Vector3 SVec = aSpeed1 - aSpeed2;
            float d = SVec.sqrMagnitude;
            // if d is 0 then the distance between Pos1 and Pos2 is never changing
            // so there is no point of closest approach... return 0
            // 0 means the closest approach is now!
            if (d >= -0.0001f && d <= 0.0002f)
                return 0.0f;
            return (-Vector3.Dot(PVec, SVec) / d);
        }

        /// <summary>
        /// returns true if first value is between second and third values (order of 2nd and 3rd doesn't matter)
        /// </summary>
        /// <param name="val"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool Between(float val, float p1, float p2)
        {
            return ((val > p1 && val < p2) || (val > p2 && val < p1));
        }

        public static bool BetweenInclusive(float val, float p1, float p2)
        {
            return ((val >= p1 && val <= p2) || (val >= p2 && val <= p1));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns either -1 or 1</returns>
        public static float Flip()
        {
            return Random.value > 0.5f ? 1 : -1;
        }

        public static int Clamp(int val, int min, int max)
        {
            val = Math.Max(min, val);
            val = Math.Min(max, val);
            return val;
        }

        public static double Max(double a, double b)
        {
            return a > b ? a : b;
        }

        public static double Clamp(double val, double min, double max)
        {
            val = Math.Max(min, val);
            val = Math.Min(max, val);
            return val;
        }
    }
}