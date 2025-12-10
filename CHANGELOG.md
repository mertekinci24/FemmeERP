# Changelog

## [v1.5.0] - 2025-11-26
### Added
- **Red Delete Button**: Enhanced UI for dangerous actions.
- **Swift Code Support**: Added Swift Code field to Cash Accounts and database migration.
- **Professional Invoice PDF**: Improved PDF generation for invoices.
- **Clean Project Structure**: Unified project structure and removed legacy artifacts.

### Fixed
- **Test Suite**: Achieved 100% pass rate (241/241 tests).
- **Foreign Key Constraints**: Resolved SQLite Error 19 issues in tests.
- **Sales Order Cancellation**: Fixed bug where cancelling a new Sales Order left a ghost draft.
- **Title Tests**: Fixed encoding issues in document title verification.

### Changed
- **Architecture**: Refactored `DocumentsViewModel` for better testability and error handling.
- **Testing**: Centralized seed data creation for ViewModel tests.
