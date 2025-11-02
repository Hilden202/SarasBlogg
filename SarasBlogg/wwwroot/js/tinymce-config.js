function initTinyMCE(selector, options = {}) {
    const baseOptions = {
        menubar: false,
        branding: false,
        height: 500,
        plugins: "lists link table code",
        toolbar:
            "undo redo | blocks | bold italic | alignleft aligncenter alignright | bullist numlist | blockquote | link | removeformat",
        block_formats: "Paragraph=p; Heading 1=h1; Heading 2=h2; Heading 3=h3",
        convert_urls: false,
        content_style: `
          body { font-family: 'Cormorant Garamond', serif; color: #7e6655; line-height: 1.6; font-size: 16px; }
          h1, h2, h3 { font-family: 'Cormorant SC', serif; color: #7e6655; text-transform: uppercase; letter-spacing: 0.05em; }
          h1 { font-size: 2rem; } h2 { font-size: 1.6rem; } h3 { font-size: 1.3rem; }
          a { color: #a87363; text-decoration: underline; }
          blockquote { border-left: 4px solid #c48a7d; margin: 1.5rem 0; padding: .75rem 1.25rem; background: #fdf3eb; }
          ul, ol { padding-left: 1.5rem; }
          table { border-collapse: collapse; width: 100%; }
          table th, table td { border: 1px solid #e2d5c3; padding: .5rem; }
          .soft-box { background: #fdf7f0; border: 1px solid #eadfd2; border-radius: 12px; padding: 1.25rem; }
          .sara-quote { border-left: 4px solid #b77966; padding: 1rem 1.5rem; background: #fff9f5; font-style: italic; }
          .image-collage { display: grid; gap: .75rem; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); }
          .image-collage img { width: 100%; height: auto; border-radius: 10px; object-fit: cover; }
        `,
        setup: (editor) => {
            editor.on("change input undo redo", () => editor.save());
        },
        ...options
    };

    tinymce.init({ selector, ...baseOptions });
}

window.addEventListener("load", () => {
    const formAdmin = document.getElementById("blogForm");
    const formAbout = document.getElementById("aboutMeForm");

    // Admin editor (med bilduppladdning)
    if (formAdmin && document.querySelector("#ContentEditor")) {
        const apiBase = (document.documentElement.dataset.apiBaseUrl || "").replace(/\/+$/, "");
        const editorToken = formAdmin?.dataset?.editorToken?.trim();

        console.log("✅ Initierar TinyMCE för Admin...");

        initTinyMCE("#ContentEditor", {
            plugins: "lists link image table code",
            toolbar:
                "undo redo | blocks | bold italic | alignleft aligncenter alignright | bullist numlist | blockquote | link image | removeformat",
            images_upload_handler: async (blobInfo) => {
                const form = document.getElementById("blogForm");
                const apiBase = (document.documentElement.dataset.apiBaseUrl || "").replace(/\/+$/, "");
                const editorToken = form?.dataset?.editorToken?.trim();
                const bloggId = form.querySelector("input[name='NewBlogg.Id']")?.value || 0;

                const uploadUrl = `${apiBase}/api/editor/upload-image?bloggId=${bloggId}`;
                const formData = new FormData();
                formData.append("file", blobInfo.blob(), blobInfo.filename());

                const response = await fetch(uploadUrl, {
                    method: "POST",
                    headers: { "Authorization": `Bearer ${editorToken}` },
                    body: formData
                });

                if (!response.ok) {
                    const err = await response.text();
                    throw new Error(`Uppladdning misslyckades (${response.status}): ${err}`);
                }

                const json = await response.json();
                return json.location;
            },
        });
    }

    // About Me editor (utan bilduppladdning)
    if (formAbout && document.querySelector("#AboutMeEditor")) {
        console.log("✅ Initierar TinyMCE för About Me...");
        initTinyMCE("#AboutMeEditor", {
            height: 400 // mindre höjd i modalen
        });
    }
});
