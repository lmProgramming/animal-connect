using System.Collections.Generic;
using System.Linq;
using Core.Configuration;
using Core.Models;

namespace Core.Logic
{
    /// <summary>
    /// Validates that path connections follow game rules.
    /// Pure functions - returns detailed validation results.
    /// </summary>
    public class ConnectionValidator
    {
        /// <summary>
        /// Validates all path connections in the network.
        /// Returns detailed information about any violations.
        /// </summary>
        public ValidationResult ValidateConnections(PathNetworkState network)
        {
            var errors = new List<ValidationError>();
            
            for (int i = 0; i < GridConfiguration.TotalPathPoints; i++)
            {
                int connectionCount = network.GetConnectionCount(i);
                bool isEntityPoint = GridConfiguration.IsEntityPoint(i);
                
                if (!GridConfiguration.IsValidConnectionCount(i, connectionCount))
                {
                    var error = CreateValidationError(i, connectionCount, isEntityPoint);
                    errors.Add(error);
                }
            }
            
            return new ValidationResult(errors);
        }
        
        /// <summary>
        /// Quick validation - just returns true/false.
        /// Faster than full validation if you don't need error details.
        /// </summary>
        public bool IsValid(PathNetworkState network)
        {
            for (int i = 0; i < GridConfiguration.TotalPathPoints; i++)
            {
                int connectionCount = network.GetConnectionCount(i);
                if (!GridConfiguration.IsValidConnectionCount(i, connectionCount))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Validates a specific path point.
        /// </summary>
        public bool IsPathPointValid(PathNetworkState network, int pathPoint)
        {
            int connectionCount = network.GetConnectionCount(pathPoint);
            return GridConfiguration.IsValidConnectionCount(pathPoint, connectionCount);
        }
        
        /// <summary>
        /// Gets all path points that have invalid connection counts.
        /// Useful for debugging and hint systems.
        /// </summary>
        public IEnumerable<int> GetInvalidPathPoints(PathNetworkState network)
        {
            for (int i = 0; i < GridConfiguration.TotalPathPoints; i++)
            {
                if (!IsPathPointValid(network, i))
                {
                    yield return i;
                }
            }
        }
        
        private ValidationError CreateValidationError(int pathPoint, int connectionCount, bool isEntityPoint)
        {
            string message;
            ErrorSeverity severity = ErrorSeverity.Error;
            
            if (isEntityPoint)
            {
                int entityIndex = GridConfiguration.GetEntityAtPoint(pathPoint);
                message = $"Entity {entityIndex} at path point {pathPoint} has {connectionCount} connections, must have exactly 1";
            }
            else
            {
                message = $"Non-entity path point {pathPoint} has {connectionCount} connections, must have 0 or 2";
                
                // Dead-ends are less severe than branch points
                if (connectionCount == 1)
                {
                    severity = ErrorSeverity.Warning;
                }
            }
            
            return new ValidationError(pathPoint, message, severity);
        }
    }
    
    /// <summary>
    /// Result of connection validation.
    /// </summary>
    public struct ValidationResult
    {
        public IReadOnlyList<ValidationError> Errors { get; }
        
        public ValidationResult(IEnumerable<ValidationError> errors)
        {
            Errors = errors?.ToList() ?? new List<ValidationError>();
        }
        
        public bool IsValid => Errors.Count == 0;
        
        public bool HasErrors => Errors.Any(e => e.Severity == ErrorSeverity.Error);
        public bool HasWarnings => Errors.Any(e => e.Severity == ErrorSeverity.Warning);
        
        public int ErrorCount => Errors.Count(e => e.Severity == ErrorSeverity.Error);
        public int WarningCount => Errors.Count(e => e.Severity == ErrorSeverity.Warning);
        
        public override string ToString()
        {
            if (IsValid)
                return "Validation: PASSED";
            
            return $"Validation: FAILED - {ErrorCount} errors, {WarningCount} warnings\n" +
                   string.Join("\n", Errors.Select(e => $"  {e}"));
        }
    }
    
    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public struct ValidationError
    {
        public int PathPoint { get; }
        public string Message { get; }
        public ErrorSeverity Severity { get; }
        
        public ValidationError(int pathPoint, string message, ErrorSeverity severity = ErrorSeverity.Error)
        {
            PathPoint = pathPoint;
            Message = message;
            Severity = severity;
        }
        
        public override string ToString()
        {
            return $"[{Severity}] Point {PathPoint}: {Message}";
        }
    }
    
    public enum ErrorSeverity
    {
        Warning,
        Error
    }
}
