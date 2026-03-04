<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="html" indent="yes" encoding="UTF-8"/>
    
    <xsl:template match="/">
        <html lang="en" class="dark">
            <head>
                <meta charset="UTF-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title><xsl:value-of select="resume/name"/> - <xsl:value-of select="resume/headline"/></title>
                <link rel="preconnect" href="https://fonts.googleapis.com" />
                <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="crossorigin" />
                <link href="https://fonts.googleapis.com/css2?family=Roboto+Condensed:wght@300;400;500;600;700&amp;display=swap" rel="stylesheet" />
                <style>
                    <![CDATA[
                    * {
                        margin: 0;
                        padding: 0;
                        box-sizing: border-box;
                    }

                    body {
                        font-family: 'Roboto Condensed', sans-serif;
                        background: #f3f4f6;
                        min-height: 100vh;
                        padding: 40px 20px;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        line-height: 1.3;
                        color: #1f2937;
                    }

                    .mx-auto {
                        max-width: 210mm;
                        margin: 0 auto;
                    }

                    /* Resume container */
                    .resume-preview-container {
                        --page-sidebar-width: 35%;
                        --page-primary-color: #475569;
                        --page-background-color: #ffffff;
                        --page-text-color: #1f2937;
                        --page-margin-x: 24px;
                        --page-margin-y: 24px;
                        --page-gap-x: 8px;
                        --page-gap-y: 8px;
                        background: var(--page-background-color);
                        color: var(--page-text-color);
                        box-shadow: 0 20px 40px rgba(0,0,0,0.1);
                        border-radius: 12px;
                        overflow: hidden;
                    }

                    /* Header */
                    .page-basics {
                        background: var(--page-primary-color);
                        color: white;
                    }

                    .basics-header {
                        display: flex;
                        align-items: center;
                    }

                    .page-picture {
                        width: 120px;
                        height: 120px;
                        border-radius: 12px;
                        overflow: hidden;
                        margin: 20px;
                        box-shadow: 0 8px 16px rgba(0,0,0,0.1);
                        border: 3px solid white;
                        background: #e2e8f0;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                    }

                    .page-picture svg {
                        width: 60px;
                        height: 60px;
                        fill: #94a3b8;
                    }

                    .basics-name {
                        font-size: 32px;
                        font-weight: 600;
                        margin-bottom: 4px;
                    }

                    .basics-headline {
                        font-size: 18px;
                        opacity: 0.9;
                        font-weight: 400;
                    }

                    .basics-items {
                        display: flex;
                        flex-wrap: wrap;
                        gap: 16px 24px;
                        padding: 16px 24px 8px 24px;
                        background: white;
                    }

                    .basics-item {
                        display: flex;
                        align-items: center;
                        gap: 8px;
                        font-size: 14px;
                    }

                    .basics-item svg {
                        width: 18px;
                        height: 18px;
                        fill: var(--page-primary-color);
                    }

                    .basics-item a, 
                    .basics-item span {
                        color: #4b5563;
                        text-decoration: none;
                    }

                    .basics-item a:hover {
                        color: var(--page-primary-color);
                        text-decoration: underline;
                    }

                    /* Main layout */
                    .flex-row {
                        display: flex;
                        padding: 24px;
                        gap: 24px;
                    }

                    /* Sidebar */
                    .page-sidebar {
                        width: var(--page-sidebar-width);
                        flex-shrink: 0;
                    }

                    .page-section {
                        margin-bottom: 24px;
                    }

                    .page-section h6 {
                        color: var(--page-primary-color);
                        font-size: 18px;
                        font-weight: 600;
                        margin-bottom: 12px;
                        padding-bottom: 4px;
                        border-bottom: 2px solid var(--page-primary-color);
                        text-transform: uppercase;
                        letter-spacing: 0.5px;
                    }

                    .section-content {
                        display: flex;
                        flex-direction: column;
                        gap: 12px;
                    }

                    .section-item {
                        break-inside: avoid;
                    }

                    .section-item-title {
                        font-weight: 600;
                        font-size: 16px;
                        color: #1f2937;
                    }

                    .section-item-metadata {
                        font-size: 14px;
                        color: #6b7280;
                    }

                    .section-item-keywords {
                        font-size: 14px;
                        color: #4b5563;
                        margin-top: 2px;
                    }

                    /* Skills */
                    .skills-item .section-item-header {
                        margin-bottom: 2px;
                    }

                    /* Languages */
                    .languages-item-header {
                        margin-bottom: 4px;
                    }

                    .language-level {
                        display: flex;
                        gap: 6px;
                        margin-top: 4px;
                    }

                    .level-dot {
                        width: 12px;
                        height: 12px;
                        border: 1px solid var(--page-primary-color);
                        border-radius: 50%;
                    }

                    .level-dot.active {
                        background: var(--page-primary-color);
                    }

                    /* Certifications */
                    .certifications-item .flex {
                        display: flex;
                        justify-content: space-between;
                        gap: 8px;
                        margin-bottom: 2px;
                    }

                    .certifications-item .section-item-website {
                        margin-top: 4px;
                    }

                    .certifications-item .section-item-website a {
                        color: var(--page-primary-color);
                        text-decoration: none;
                        font-size: 13px;
                    }

                    /* Main content */
                    .page-main {
                        flex: 1;
                    }

                    /* Summary */
                    ._tiptap_content_11g7w_1 {
                        font-size: 14px;
                        line-height: 1.6;
                        color: #4b5563;
                    }

                    ._tiptap_content_11g7w_1 p {
                        margin-bottom: 12px;
                    }

                    ._tiptap_content_11g7w_1 ul {
                        margin: 12px 0;
                        padding-left: 24px;
                    }

                    ._tiptap_content_11g7w_1 li {
                        margin-bottom: 6px;
                    }

                    ._tiptap_content_11g7w_1 strong {
                        color: #1f2937;
                    }

                    /* Experience */
                    .experience-item .flex {
                        display: flex;
                        justify-content: space-between;
                        gap: 12px;
                        margin-bottom: 2px;
                    }

                    .experience-item .section-item-metadata {
                        text-align: right;
                    }

                    .experience-item .section-item-description {
                        margin-top: 8px;
                    }

                    /* Education */
                    .education-item .flex {
                        display: flex;
                        justify-content: space-between;
                        gap: 12px;
                        margin-bottom: 2px;
                    }

                    .education-item .section-item-metadata {
                        text-align: right;
                    }

                    /* Utilities */
                    .shrink-0 {
                        flex-shrink: 0;
                    }

                    .text-end {
                        text-align: right;
                    }

                    .opacity-80 {
                        opacity: 0.8;
                    }

                    .inline-block {
                        display: inline-block;
                    }

                    .break-all {
                        word-break: break-all;
                    }

                    .gap-x-2 {
                        column-gap: 8px;
                    }

                    .gap-x-3 {
                        column-gap: 12px;
                    }

                    .gap-y-1 {
                        row-gap: 4px;
                    }

                    .mt-2 {
                        margin-top: 8px;
                    }

                    .mb-1\.5 {
                        margin-bottom: 6px;
                    }

                    .mb-2 {
                        margin-bottom: 8px;
                    }

                    .pt-3 {
                        padding-top: 12px;
                    }

                    .px-3 {
                        padding-left: 12px;
                        padding-right: 12px;
                    }

                    .px-\(--page-margin-x\) {
                        padding-left: var(--page-margin-x);
                        padding-right: var(--page-margin-x);
                    }

                    .py-\(--page-margin-y\) {
                        padding-top: var(--page-margin-y);
                        padding-bottom: var(--page-margin-y);
                    }

                    .pt-\(--page-margin-y\) {
                        padding-top: var(--page-margin-y);
                    }

                    .space-y-4 > * + * {
                        margin-top: 16px;
                    }

                    /* Print styles */
                    @media print {
                        body {
                            padding: 0;
                            background: white;
                        }
                        .resume-preview-container {
                            box-shadow: none;
                            border-radius: 0;
                        }
                    }
                    ]]>
                </style>
            </head>
            <body>
                <div class="mx-auto">
                    <xsl:apply-templates select="resume"/>
                </div>
            </body>
        </html>
    </xsl:template>
    
    <xsl:template match="resume">
        <div class="resume-preview-container space-y-4">
            <div class="relative page">
                <div class="template-ditto page-content">
                    <!-- Header -->
                    <div class="page-header">
                        <div class="page-basics">
                            <div class="basics-header">
                                <div style="width: var(--page-sidebar-width); flex-shrink: 0; display: flex; justify-content: center;">
                                    <div class="page-picture">
                                        <xsl:choose>
                                            <xsl:when test="picturePath != '' and picturePath != 'data:,'">
                                                <img style="width:120px;height:120px;" src="{picturePath}" alt="Profile picture"/>
                                            </xsl:when>
                                            <xsl:otherwise>
                                                <svg viewBox="0 0 24 24" fill="currentColor">
                                                    <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
                                                </svg>
                                            </xsl:otherwise>
                                        </xsl:choose>
                                    </div>
                                </div>
                                <div class="px-(--page-margin-x) py-(--page-margin-y)">
                                    <h2 class="basics-name"><xsl:value-of select="name"/></h2>
                                    <p class="basics-headline"><xsl:value-of select="headline"/></p>
                                </div>
                            </div>
                        </div>
                        
                        <!-- Contact Items -->
                        <div class="flex items-center">
                            <div class="w-(--page-sidebar-width) shrink-0"></div>
                            <div class="basics-items flex flex-wrap gap-x-3 gap-y-1 px-(--page-margin-x) pt-3">
                                <xsl:if test="email != ''">
                                    <div class="basics-item">
                                        <svg xmlns="http://www.w3.org/2000/svg" width="15.75" height="15.75" fill="var(--page-primary-color)" viewBox="0 0 256 256">
                                            <path d="M224,48H32a8,8,0,0,0-8,8V192a16,16,0,0,0,16,16H216a16,16,0,0,0,16-16V56A8,8,0,0,0,224,48Zm-96,85.15L52.57,64H203.43ZM98.71,128,40,181.81V74.19Zm11.84,10.85,12,11.05a8,8,0,0,0,10.82,0l12-11.05,58,53.15H52.57ZM157.29,128,216,74.18V181.82Z"></path>
                                        </svg>
                                        <a href="mailto:{email}" target="_blank" rel="noopener" class="inline-block text-wrap break-all"><xsl:value-of select="email"/></a>
                                    </div>
                                </xsl:if>
                                
                                <xsl:if test="phone != ''">
                                    <div class="basics-item">
                                        <svg xmlns="http://www.w3.org/2000/svg" width="15.75" height="15.75" fill="var(--page-primary-color)" viewBox="0 0 256 256">
                                            <path d="M222.37,158.46l-47.11-21.11-.13-.06a16,16,0,0,0-15.17,1.4,8.12,8.12,0,0,0-.75.56L134.87,160c-15.42-7.49-31.34-23.29-38.83-38.51l20.78-24.71c.2-.25.39-.5.57-.77a16,16,0,0,0,1.32-15.06l0-.12L97.54,33.64a16,16,0,0,0-16.62-9.52A56.26,56.26,0,0,0,32,80c0,79.4,64.6,144,144,144a56.26,56.26,0,0,0,55.88-48.92A16,16,0,0,0,222.37,158.46ZM176,208A128.14,128.14,0,0,1,48,80a40.2,40.2,0,0,1,34.87-40,.61.61,0,0,0,0,.12l21,47L83.2,111.86a6.13,6.13,0,0,0-.57.77,16,16,0,0,0-1,15.7c9.06,18.53,27.73,37.06,46.46,46.11a16,16,0,0,0,15.75-1.14,8.44,8.44,0,0,0,.74-.56L168.89,152l47,21.05h0s.08,0,.11,0A40.21,40.21,0,0,1,176,208Z"></path>
                                        </svg>
                                        <a href="tel:{phone}" target="_blank" rel="noopener" class="inline-block text-wrap break-all"><xsl:value-of select="phone"/></a>
                                    </div>
                                </xsl:if>
                                
                                <xsl:if test="location != ''">
                                    <div class="basics-item">
                                        <svg xmlns="http://www.w3.org/2000/svg" width="15.75" height="15.75" fill="var(--page-primary-color)" viewBox="0 0 256 256">
                                            <path d="M128,64a40,40,0,1,0,40,40A40,40,0,0,0,128,64Zm0,64a24,24,0,1,1,24-24A24,24,0,0,1,128,128Zm0-112a88.1,88.1,0,0,0-88,88c0,31.4,14.51,64.68,42,96.25a254.19,254.19,0,0,0,41.45,38.3,8,8,0,0,0,9.18,0A254.19,254.19,0,0,0,174,200.25c27.45-31.57,42-64.85,42-96.25A88.1,88.1,0,0,0,128,16Zm0,206c-16.53-13-72-60.75-72-118a72,72,0,0,1,144,0C200,161.23,144.53,209,128,222Z"></path>
                                        </svg>
                                        <span class="inline-block text-wrap break-all"><xsl:value-of select="location"/></span>
                                    </div>
                                </xsl:if>
                                
                                <!-- Custom links -->
                                <xsl:for-each select="links/link">
                                    <div class="basics-item">
                                        <svg xmlns="http://www.w3.org/2000/svg" width="15.75" height="15.75" fill="var(--page-primary-color)" viewBox="0 0 256 256">
                                            <path d="M128,24h0A104,104,0,1,0,232,128,104.12,104.12,0,0,0,128,24Zm88,104a87.61,87.61,0,0,1-3.33,24H174.16a157.44,157.44,0,0,0,0-48h38.51A87.61,87.61,0,0,1,216,128ZM102,168H154a115.11,115.11,0,0,1-26,45A115.27,115.27,0,0,1,102,168Zm-3.9-16a140.84,140.84,0,0,1,0-48h59.88a140.84,140.84,0,0,1,0,48ZM40,128a87.61,87.61,0,0,1,3.33-24H81.84a157.44,157.44,0,0,0,0,48H43.33A87.61,87.61,0,0,1,40,128ZM154,88H102a115.11,115.11,0,0,1,26-45A115.27,115.27,0,0,1,154,88Zm52.33,0H170.71a135.28,135.28,0,0,0-22.3-45.6A88.29,88.29,0,0,1,206.37,88ZM107.59,42.4A135.28,135.28,0,0,0,85.29,88H49.63A88.29,88.29,0,0,1,107.59,42.4ZM49.63,168H85.29a135.28,135.28,0,0,0,22.3,45.6A88.29,88.29,0,0,1,49.63,168Zm98.78,45.6a135.28,135.28,0,0,0,22.3-45.6h35.66A88.29,88.29,0,0,1,148.41,213.6Z"></path>
                                        </svg>
                                        <a href="{.}" target="_blank" rel="noopener" class="inline-block text-wrap break-all">
                                            <xsl:choose>
                                                <xsl:when test="contains(., 'linkedin')">LinkedIn</xsl:when>
                                                <xsl:when test="contains(., 'github')">GitHub</xsl:when>
                                                <xsl:otherwise>Website</xsl:otherwise>
                                            </xsl:choose>
                                        </a>
                                    </div>
                                </xsl:for-each>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Main Content with Sidebar -->
                    <div class="flex-row flex pt-(--page-margin-y)">
                        <!-- Sidebar -->
                        <aside class="page-sidebar group space-y-4">
                            <!-- Skills -->
                            <xsl:if test="skills/skill">
                                <section class="page-section">
                                    <h6 class="mb-1.5 text-(--page-primary-color)">Skills</h6>
                                    <div class="section-content grid">
                                        <xsl:apply-templates select="skills/skill"/>
                                    </div>
                                </section>
                            </xsl:if>
                            
                            <!-- Certifications -->
                            <xsl:if test="certifications/certification">
                                <section class="page-section">
                                    <h6 class="mb-1.5 text-(--page-primary-color)">Certifications</h6>
                                    <div class="section-content grid">
                                        <xsl:apply-templates select="certifications/certification"/>
                                    </div>
                                </section>
                            </xsl:if>
                            
                            <!-- Languages -->
                            <xsl:if test="languages/language">
                                <section class="page-section">
                                    <h6 class="mb-1.5 text-(--page-primary-color)">Languages</h6>
                                    <div class="section-content grid">
                                        <xsl:apply-templates select="languages/language"/>
                                    </div>
                                </section>
                            </xsl:if>
                        </aside>
                        
                        <!-- Main Content -->
                        <main class="page-main group space-y-4">
                            <!-- Summary -->
                            <xsl:if test="summary != ''">
                                <section class="page-section">
                                    <h6 class="mb-1.5 text-(--page-primary-color)">Summary</h6>
                                    <div class="section-content">
                                        <div class="_tiptap_content_11g7w_1">
                                            <xsl:value-of select="summary" disable-output-escaping="yes"/>
                                        </div>
                                    </div>
                                </section>
                            </xsl:if>
                            
                            <!-- Experience -->
                            <xsl:if test="experiences/experience">
                                <section class="page-section">
                                    <h6 class="mb-1.5 text-(--page-primary-color)">Experience</h6>
                                    <div class="section-content grid">
                                        <xsl:apply-templates select="experiences/experience"/>
                                    </div>
                                </section>
                            </xsl:if>
                            
                            <!-- Education -->
                            <xsl:if test="educations/education">
                                <section class="page-section">
                                    <h6 class="mb-1.5 text-(--page-primary-color)">Education</h6>
                                    <div class="section-content grid">
                                        <xsl:apply-templates select="educations/education"/>
                                    </div>
                                </section>
                            </xsl:if>
                        </main>
                    </div>
                </div>
            </div>
        </div>
    </xsl:template>
    

    <xsl:template match="skill">
        <div class="section-item">
            <div class="skills-item">
                <div class="section-item-header flex flex-col">
                    <strong class="section-item-title"><xsl:value-of select="name"/></strong>
                </div>
                <xsl:if test="keywords/keyword">
                    <div class="section-item-keywords opacity-80">
                        <xsl:for-each select="keywords/keyword">
                            <xsl:value-of select="."/>
                            <xsl:if test="position() != last()">, </xsl:if>
                        </xsl:for-each>
                    </div>
                </xsl:if>
            </div>
        </div>
    </xsl:template>

    <xsl:template match="certification">
        <div class="section-item">
            <div class="certifications-item">
                <div class="section-item-header">
                    <div class="flex items-start justify-between gap-x-2">
                        <strong class="section-item-title"><xsl:value-of select="title"/></strong>
                        <xsl:if test="date != ''">
                            <span class="section-item-metadata shrink-0 text-end"><xsl:value-of select="date"/></span>
                        </xsl:if>
                    </div>
                    <xsl:if test="issuer != ''">
                        <div class="flex items-start justify-between gap-x-2">
                            <span class="section-item-metadata"><xsl:value-of select="issuer"/></span>
                        </div>
                    </xsl:if>
                </div>
                <xsl:if test="website != ''">
                    <div class="section-item-website mt-2">
                        <a href="{website/OriginalString}" target="_blank" rel="noopener" class="inline-block text-wrap break-all">
                            <xsl:value-of select="label"/>
                        </a>
                    </div>
                </xsl:if>
            </div>
        </div>
    </xsl:template>

    <xsl:template match="language">
        <div class="section-item">
            <div class="languages-item">
                <div class="section-item-header flex flex-col">
                    <strong class="section-item-title"><xsl:value-of select="name"/></strong>
                    <xsl:if test="fluency != ''">
                        <span class="section-item-metadata opacity-80"><xsl:value-of select="fluency"/></span>
                    </xsl:if>
                </div>
                <div class="language-level" aria-label="Level {level} of 5">
                    <xsl:call-template name="language-level">
                        <xsl:with-param name="level" select="level"/>
                    </xsl:call-template>
                </div>
            </div>
        </div>
    </xsl:template>

    <xsl:template match="experience">
        <div class="section-item">
            <div class="experience-item">
                <div class="section-item-header">
                    <div class="flex items-start justify-between gap-x-2">
                        <xsl:choose>
                            <xsl:when test="website != ''">
                                <a href="{website/OriginalString}" target="_blank" rel="noopener" class="inline-block section-item-title">
                                    <strong><xsl:value-of select="company"/></strong>
                                </a>
                            </xsl:when>
                            <xsl:otherwise>
                                <strong class="section-item-title"><xsl:value-of select="company"/></strong>
                            </xsl:otherwise>
                        </xsl:choose>
                        <xsl:if test="location != ''">
                            <span class="section-item-metadata shrink-0 text-end"><xsl:value-of select="location"/></span>
                        </xsl:if>
                    </div>
                    <div class="flex items-start justify-between gap-x-2">
                        <span class="section-item-metadata"><xsl:value-of select="position"/></span>
                        <xsl:if test="period != ''">
                            <span class="section-item-metadata shrink-0 text-end"><xsl:value-of select="period"/></span>
                        </xsl:if>
                    </div>
                </div>
                <xsl:if test="description != ''">
                    <div class="section-item-description">
                        <div class="_tiptap_content_11g7w_1">
                            <xsl:value-of select="description" disable-output-escaping="yes"/>
                        </div>
                    </div>
                </xsl:if>
            </div>
        </div>
    </xsl:template>

    <xsl:template match="education">
        <div class="section-item">
            <div class="education-item">
                <div class="section-item-header mb-2">
                    <div class="flex items-start justify-between gap-x-2">
                        <xsl:choose>
                            <xsl:when test="website != ''">
                                <a href="{website}" target="_blank" rel="noopener" class="inline-block section-item-title">
                                    <strong><xsl:value-of select="school"/></strong>
                                </a>
                            </xsl:when>
                            <xsl:otherwise>
                                <strong class="section-item-title"><xsl:value-of select="school"/></strong>
                            </xsl:otherwise>
                        </xsl:choose>
                        <xsl:if test="degree != '' or grade != ''">
                            <span class="section-item-metadata shrink-0 text-end">
                                <xsl:value-of select="degree"/>
                                <xsl:if test="grade != ''"> • <xsl:value-of select="grade"/></xsl:if>
                            </span>
                        </xsl:if>
                    </div>
                    <div class="flex items-start justify-between gap-x-2">
                        <xsl:if test="area != ''">
                            <span class="section-item-metadata"><xsl:value-of select="area"/></span>
                        </xsl:if>
                        <xsl:if test="location != '' or period != ''">
                            <span class="section-item-metadata shrink-0 text-end">
                                <xsl:value-of select="location"/>
                                <xsl:if test="location != '' and period != ''"> • </xsl:if>
                                <xsl:value-of select="period"/>
                            </span>
                        </xsl:if>
                    </div>
                </div>
            </div>
        </div>
    </xsl:template>            

    <xsl:template name="language-level">
        <xsl:param name="level"/>
        
        <!-- Dot 1 -->
        <div class="level-dot">
            <xsl:if test="$level &gt;= 1">
                <xsl:attribute name="class">level-dot active</xsl:attribute>
            </xsl:if>
        </div>
        
        <!-- Dot 2 -->
        <div class="level-dot">
            <xsl:if test="$level &gt;= 2">
                <xsl:attribute name="class">level-dot active</xsl:attribute>
            </xsl:if>
        </div>
        
        <!-- Dot 3 -->
        <div class="level-dot">
            <xsl:if test="$level &gt;= 3">
                <xsl:attribute name="class">level-dot active</xsl:attribute>
            </xsl:if>
        </div>
        
        <!-- Dot 4 -->
        <div class="level-dot">
            <xsl:if test="$level &gt;= 4">
                <xsl:attribute name="class">level-dot active</xsl:attribute>
            </xsl:if>
        </div>
        
        <!-- Dot 5 -->
        <div class="level-dot">
            <xsl:if test="$level &gt;= 5">
                <xsl:attribute name="class">level-dot active</xsl:attribute>
            </xsl:if>
        </div>
    </xsl:template>
    
</xsl:stylesheet>