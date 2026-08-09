# Formula validation latency evidence

The automated guard in `FormulaValidationLatencyTests` runs 100 representative
in-limit compilations with a monotonic `Stopwatch`, excludes no hidden network
dependency, and records the nearest-rank p95 calculation in the test. The
threshold from SC-005 remains a delivery acceptance gate to be measured on the
supported local profile.
