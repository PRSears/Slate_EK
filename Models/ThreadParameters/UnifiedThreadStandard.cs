using Extender;
using Extender.Debugging;
using Extender.UnitConversion;
using Extender.UnitConversion.Lengths;
using Slate_EK.Models.IO;
using System;
using System.Linq;
using System.Xml.Serialization;

namespace Slate_EK.Models.ThreadParameters
{
    public class UnifiedThreadStandard
    {
        private const double TOLERANCE = 0.000001d;

        /// <summary>
        /// Gets or sets the designation label for this entry of UTS
        /// </summary>
        public string Designation { get; set; }

        /// <summary>
        /// Gets or sets the nominal outer (major) diameter (in inches).
        /// </summary>
        public float MajorDiameter { get; set; }

        /// <summary>
        /// Gets or sets the UNC thread pitch (in inches) for this designation.
        /// </summary>
        [XmlIgnore]
        public float CourseThreadPitch { get; set; }

        /// <summary>
        /// Gets or sets the UNF thread pitch (in inches) for this designation.
        /// </summary>
        [XmlIgnore]
        public float FineThreadPitch { get; set; }

        /// <summary>
        /// Gets or sets the UNEF thread pitch (in inches) for this designation.
        /// </summary>
        [XmlIgnore]
        public float ExtraFineThreadPitch { get; set; }

        /// <summary>
        /// Gets or sets the number of threads per inch in the UNC description for this designation. 
        /// </summary>
        public float UncThreadsPerInch
        {
            get { return CourseThreadPitch == 0 ? 0 : (float)Math.Round(1f / CourseThreadPitch); }
            set { CourseThreadPitch = (float)Math.Round(1f / value, 6); }
        }

        /// <summary>
        /// Gets or sets the number of threads per inch in the UNF description for this designation. 
        /// </summary>
        public float UnfThreadsPerInch
        {
            get { return FineThreadPitch == 0 ? 0 : (float)Math.Round(1f / FineThreadPitch); }
            set { FineThreadPitch = (float)Math.Round(1f / value, 6); }
        }

        /// <summary>
        /// Gets or sets the number of threads per inch in the UNEF description for this designation.
        /// </summary>
        public float UnefThreadsPerInch
        {
            get { return ExtraFineThreadPitch == 0 ? 0 : (float)Math.Round(1f / ExtraFineThreadPitch); }
            set { ExtraFineThreadPitch = (float)Math.Round(1f / value, 6); }
        }

        /// <summary>
        /// Gets the thread density in threads per inch for this UTS designation with specified 
        /// Unified Screw Thread designation. 
        /// </summary>
        public float GetThreadDensity(ThreadDensity threadDensity)
        {
            switch (threadDensity)
            {
                case ThreadDensity.UNC:
                    return UncThreadsPerInch;
                case ThreadDensity.UNF:
                    return UnfThreadsPerInch;
                case ThreadDensity.UNEF:
                    return UnefThreadsPerInch;
            }

            return 0f;
        }

        /// <summary>
        /// Gets the thread density in threads per inch for this UTS designation with specified 
        /// Unified Screw Thread designation. 
        /// </summary>
        public float GetThreadDensity(string threadDensity)
        {
            if (string.IsNullOrWhiteSpace(threadDensity)) return 0f;

            int tpi;

            // Check if the provided string designation is all numbers
            if (!int.TryParse(threadDensity, out tpi))
            {
                return GetThreadDensity // it's not all numbers so we try to parse the designation (UNC / UNF / UNEF)
                (
                    ((ThreadDensity[])Enum.GetValues(typeof(ThreadDensity)))
                                          .First(d => threadDensity.StartsWith(Enum.GetName(typeof(ThreadDensity), d)))
                );
            }

            if (UncThreadsPerInch.RoughEquals(tpi, TOLERANCE) || 
                UnfThreadsPerInch.RoughEquals(tpi, TOLERANCE) || 
                UnefThreadsPerInch.RoughEquals(tpi, TOLERANCE))
            {
                return tpi; // if it's already a valid TPI
            }

            // Exception is thrown when threadDensity didn't contain text, and was not a valid TPI
            throw new InvalidOperationException($"Thread density '{threadDensity}' could not be found.");
        }

        /// <summary>
        /// Creates a string display of the ThreadDensity designation with it's associated TPI value.
        /// </summary>
        /// <param name="threadDensity"></param>
        public string GetThreadDensityDisplay(ThreadDensity threadDensity)
        {
            return $"{Enum.GetName(typeof(ThreadDensity), threadDensity)?.PadRight(4)} - {GetThreadDensity(threadDensity)} TPI";
        }

        /// <summary> 
        /// Creates a string display of the ThreadDensity designation with it's associated TPI value.
        /// </summary>
        /// <param name="pitchInMillimeters">The pitch in millimeters specifying which ThreadDensity designation to 
        /// generate the string for.</param>
        public string GetThreadDensityDisplay(float pitchInMillimeters)
        {
            float convert = (float)Measure.Convert<Millimeter, Inch>(pitchInMillimeters);
            float tpi     = (float)Math.Round(1f / convert);

            return GetThreadDensityDisplay
            (
                ((ThreadDensity[])Enum.GetValues(typeof(ThreadDensity)))
                                      .First(d => GetThreadDensity(d).RoughEquals(tpi, 0.5))
            );
        }

        /// <summary>
        /// Looks up the appropriate pitch for the diameter and thread density provided.
        /// </summary>
        /// <param name="diamterInInches">The major diameter, in inches, of the fastener to look up.</param>
        /// <param name="density">The thread density option to search for.</param>
        /// <returns></returns>
        public static float LookupThreadPitch(float diamterInInches, ThreadDensity density)
        {
            var match = ImperialSizesCache.Table?.FirstOrDefault(f => f.MajorDiameter.RoughEquals(diamterInInches, TOLERANCE));

            if (match == null || match.Equals(default(UnifiedThreadStandard)))
            {
                Debug.WriteMessage($"LookupThreadPitch() could not find a UnifiedThreadStandard matching {diamterInInches}.", "warn");
                return 0f;
            }

            return match.GetThreadDensity(density);
        }

        /// <summary>
        /// Looks up a UnifiedThreadStandard from the ImperialSizesCache with a major diameter
        /// matching the one provided.
        /// </summary>
        /// <param name="diameterInMillimeters">The diameter provided in millimeters to search for.</param>
        /// <returns>
        /// A UnifiedThreadStandard object with a major diameter matching the one provided.
        /// Returns null if none are found.
        /// </returns>
        public static UnifiedThreadStandard FromMillimeters(float diameterInMillimeters)
        {
            Inch diameter = new Millimeter(diameterInMillimeters).ConvertTo<Inch>();
            
            return Lookup(f => f.MajorDiameter.RoughEquals(diameter.Value, TOLERANCE));
        }

        /// <summary>
        /// Looks up a UnifiedThreadStandard from the ImperialSizesCache with a designation
        /// matching the one provided.
        /// </summary>
        /// <param name="designation">The UTS designation describing the desired UnifiedThreadStandard.</param>
        /// <returns>
        /// A UnifiedThreadStandard object with a designation matching the one provided.
        /// Returns null if none are found.
        /// </returns>
        public static UnifiedThreadStandard FromDesignation(string designation)
        {
            return Lookup(f => f.Designation.Equals(designation));
        }

        /// <summary>
        /// Looks up a UnifiedThreadStandard from the ImperialSizesCache with a 
        /// major diameter matching the one provided.
        /// </summary>
        /// <param name="diameter">Major diameter (outer diameter) describing the desired UnifiedThreadStandard object,
        /// in inches. </param>
        /// <returns>A UnifiedThreadStandard object with a major diameter matching the one provided.
        /// Returns null if none are found.</returns>
        public static UnifiedThreadStandard FromInches(Inch diameter)
        {
            return Lookup(f => f.MajorDiameter.RoughEquals(diameter.Value, TOLERANCE));
        }

        /// <summary>
        /// Looks up a UnifiedThreadStandard from the ImperialSizes cache that satisfies 
        /// a condition, or null if none are found. 
        /// </summary>
        /// <param name="predicate">A function to test each element of the ImperialSizesCache for a condition.</param>
        /// <returns></returns>
        public static UnifiedThreadStandard Lookup(Func<UnifiedThreadStandard, bool> predicate)
        {
            var match = ImperialSizesCache.Table?.FirstOrDefault(predicate);

            if (match == null || match.Equals(default(UnifiedThreadStandard)))
                Debug.WriteMessage(
                    $"UnifiedThreadStandard.LookUp failed to find a fastener that satisfied the predicate.", "warn");

            return match;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (!(obj is UnifiedThreadStandard)) return false;

            var b = (UnifiedThreadStandard)obj;

            return this.Designation.Equals(b.Designation)                                   &&
                   this.MajorDiameter.RoughEquals(b.MajorDiameter, TOLERANCE)               &&
                   this.CourseThreadPitch.RoughEquals(b.CourseThreadPitch, TOLERANCE)       &&
                   this.FineThreadPitch.RoughEquals(b.FineThreadPitch, TOLERANCE)           &&
                   this.ExtraFineThreadPitch.RoughEquals(b.ExtraFineThreadPitch, TOLERANCE);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return Designation;
        }

        public static bool operator ==(UnifiedThreadStandard a, UnifiedThreadStandard b)
        {
            return ReferenceEquals(null, a) ? ReferenceEquals(null, b) : a.Equals(b);
        }

        public static bool operator !=(UnifiedThreadStandard a, UnifiedThreadStandard b)
        {
            return !(a == b);
        }
    }
    
    /// <summary>
    /// Types of thread densities as described by the Unified Thread Standard designations.
    /// </summary>
    public enum ThreadDensity { UNC, UNF, UNEF }
}
