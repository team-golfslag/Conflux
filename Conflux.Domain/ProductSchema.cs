// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

namespace Conflux.Domain;

public enum ProductSchema
{
    /// <seealso href="https://arks.org/"/>
    Ark,
    /// <summary>
    /// all DOIs, including IGSNs, CrossRef Publication IDs or Grant IDs, DataCite DOIs, instrument DOIs, etc.
    /// </summary>
    /// <seealso href="https://doi.org/"/>
    Doi,
    /// <summary>
    /// all non-DOI handles
    /// </summary>
    /// <seealso href="http://hdl.handle.net/"/>
    Handle,
    /// <seealso href="https://www.isbn-international.org/"/>
    Isbn,
    /// <seealso href="https://scicrunch.org/resolver/"/>
    Rrid,
    /// <summary>
    /// fallback for any Object that has no ID other than a webpage - a snapshot must be taken from archive.org and that link inserted into the RAiD
    /// </summary>
    /// <seealso href="https://archive.org/"/>
    Archive,
}
