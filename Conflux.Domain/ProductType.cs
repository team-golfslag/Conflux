// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain;

/// <summary>
/// RAiD “relatedObject.type” controlled vocabulary
/// (see https://vocabulary.raid.org/relatedObject.type.schema/*).
/// [oai_citation:0‡metadata.raid.org](https://metadata.raid.org/en/latest/core/relatedObjects.html)
/// </summary>
public enum ProductType
{
    /// <summary>
    /// Audio-visual materials such as video recordings or podcasts.
    /// </summary>
    Audiovisual = 273,

    /// <summary>
    /// Monograph or edited volume with an ISBN.
    /// </summary>
    Book = 258,

    /// <summary>
    /// A chapter or section within a book.
    /// </summary>
    BookChapter = 271,

    /// <summary>
    /// Executable notebook that combines code, text and rich media (e.g. Jupyter).
    /// </summary>
    ComputationalNotebook = 256,

    /// <summary>
    /// Paper published in peer-reviewed conference proceedings.
    /// </summary>
    ConferencePaper = 264,

    /// <summary>
    /// Poster presented at a conference.
    /// </summary>
    ConferencePoster = 248,

    /// <summary>
    /// Complete conference proceedings volume.
    /// </summary>
    ConferenceProceeding = 262,

    /// <summary>
    /// Article whose main content is a dataset description.
    /// </summary>
    DataPaper = 255,

    /// <summary>
    /// A dataset or collection of datasets.
    /// </summary>
    Dataset = 269,

    /// <summary>
    /// Doctoral, masters or other academic thesis / dissertation.
    /// </summary>
    Dissertation = 253,

    /// <summary>
    /// An event such as a workshop, meeting or seminar.
    /// </summary>
    Event = 260,

    /// <summary>
    /// Financial award such as a grant (excludes prizes).
    /// </summary>
    Funding = 272,

    /// <summary>
    /// Still image, photograph, figure or diagram.
    /// </summary>
    Image = 257,

    /// <summary>
    /// Scientific or technical instrument or piece of equipment.
    /// </summary>
    Instrument = 266,

    /// <summary>
    /// Article published in a scholarly journal.
    /// </summary>
    JournalArticle = 250,

    /// <summary>
    /// Object designed for learning or teaching, e.g. tutorial or lecture slides.
    /// </summary>
    LearningObject = 267,

    /// <summary>
    /// Mathematical, conceptual or computational model.
    /// </summary>
    Model = 263,

    /// <summary>
    /// Data/Output Management Plan or equivalent project-level plan.
    /// </summary>
    OutputManagementPlan = 247,

    /// <summary>
    /// Tangible, physical artefact produced or used by the project.
    /// </summary>
    PhysicalObject = 270,

    /// <summary>
    /// Scholarly work made public before formal peer review.
    /// </summary>
    Preprint = 254,

    /// <summary>
    /// Award that recognises achievement but is not accompanied by funding.
    /// </summary>
    Prize = 268,

    /// <summary>
    /// Technical, project or institutional report.
    /// </summary>
    Report = 252,

    /// <summary>
    /// Research support service or service output.
    /// </summary>
    Service = 274,

    /// <summary>
    /// Computer program, library or script.
    /// </summary>
    Software = 259,

    /// <summary>
    /// Audio-only resource such as sound recordings.
    /// </summary>
    Sound = 261,

    /// <summary>
    /// Standard, specification or protocol.
    /// </summary>
    Standard = 251,

    /// <summary>
    /// Narrative text that does not fit a more specific type.
    /// </summary>
    Text = 265,

    /// <summary>
    /// Workflow description, pipeline or protocol.
    /// </summary>
    Workflow = 249,
}
