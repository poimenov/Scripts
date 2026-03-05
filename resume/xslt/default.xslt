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
                        font-family: 'Roboto Condensed', Helvetica Condensed;
                        background: #f3f4f6;
                        min-height: 100vh;
                        padding: 20px;
                        font-size:14px;
                        line-height: 1.3;
                        color: #1f2937;
                    }

                    .mx-auto {
                        max-width: 210mm;
                        margin: 0 auto;
                        width: 100%;
                    }

                    .resume-preview-container {
                        --sidebar-width: 35%;
                        --primary-color: #475569;
                        --bg-color: #ffffff;
                        --text-color: #1f2937;
                        background: var(--bg-color);
                        color: var(--text-color);
                        box-shadow: 0 20px 40px rgba(0,0,0,0.1);
                        border-radius: 12px;
                        overflow: hidden;
                        width: 100%;
                    }

                    /* Header */
                    .page-header {
                        background: var(--primary-color);
                        color: white;
                    }

                    .header-main {
                        display: flex;
                        align-items: center;
                        padding: 12px;
                    }

                    .profile-picture {
                        width: var(--sidebar-width);
                        height: 120px;
			            text-align: center;
                    }

                    .profile-picture img {
                        width: 120px;
                        height: 120px;
                        border-radius: 12px;
                        border: 1px solid white;			
                    }

                    .profile-info {
                        flex: 1;
                    }

                    .profile-name {
                        font-size: 32px;
                        font-weight: 600;
                        margin-bottom: 4px;
                    }

                    .profile-headline {
                        font-size: 18px;
                        opacity: 0.9;
                        font-weight: 400;
                    }

                    /* Contact bar */
                    .contact-bar {
                        background: white;
                        padding: 16px;
                        border-bottom: 1px solid #e5e7eb;
                    }

                    .contact-items {
                        display: flex;
                        flex-wrap: wrap;
                        gap: 16px 24px;
                    }

                    .contact-item {
                        display: flex;
                        align-items: center;
                        gap: 8px;
                        font-size: 14px;
                    }

                    .contact-item svg {
                        width: 18px;
                        height: 18px;
                        fill: var(--primary-color);
                        flex-shrink: 0;
                    }

                    .contact-item a,
                    .contact-item span {
                        color: #4b5563;
                        text-decoration: none;
                    }

                    .contact-item a:hover {
                        color: var(--primary-color);
                        text-decoration: underline;
                    }

                    /* Main layout */
                    .main-layout {
                        display: flex;
                        padding: 12px;
                        gap: 12px;
                    }

                    .sidebar {
                        width: var(--sidebar-width);
                        flex-shrink: 0;
                    }

                    .page-section {
                        margin-bottom: 12px;
                    }

                    .page-section h6 {
                        color: var(--primary-color);
                        font-size: 18px;
                        font-weight: 600;
                        margin-bottom: 10px;
                        padding-bottom: 2px;
                        border-bottom: 2px solid var(--primary-color);
                        text-transform: uppercase;
                        letter-spacing: 0.5px;
                    }                    

                    .content {
                        flex: 1;
                    }

                    /* Sections */
                    .section {
                        margin-bottom: 24px;
                    }

                    .section-title {
                        color: var(--primary-color);
                        font-size: 18px;
                        font-weight: 600;
                        margin-bottom: 12px;
                        padding-bottom: 4px;
                        border-bottom: 2px solid var(--primary-color);
                        text-transform: uppercase;
                        letter-spacing: 0.5px;
                    }

                    .section-items {
                        display: flex;
                        flex-direction: column;
                        gap: 12px;
                    }

                    .section-item {
                        padding-bottom: 4px;
                    }

                    .section-content p {
                        margin-bottom: 6px;
                    }

                    .section-content ul {
                        margin: 6px 0;
                        padding-left: 24px;
                    }

                    .section-content li {
                        margin-bottom: 6px;
                    }

                    .section-content strong {
                        color: #1f2937;
                    }                    

                    .item-title {
                        font-weight: 600;
                        font-size: 16px;
                        color: #1f2937;
                    }

                    .item-metadata {
                        font-size: 14px;
                        color: #6b7280;
                    }

                    .item-keywords {
                        font-size: 14px;
                        color: #4b5563;
                        margin-top: 2px;
                    }

                    /* Languages */
                    .language-level {
                        display: flex;
                        gap: 6px;
                        margin-top: 4px;
                        margin-bottom: 12px;
                    }

                    .level-dot {
                        width: 12px;
                        height: 12px;
                        border: 1px solid var(--primary-color);
                        border-radius: 50%;
                    }

                    .level-dot.active {
                        background: var(--primary-color);
                    }

                    /* Certifications */
                    .cert-row {
                        display: flex;
                        justify-content: space-between;
                        gap: 8px;
                        margin-bottom: 2px;
                    }

                    .cert-website {
                        margin-top: 4px;
                    }

                    .cert-website a {
                        color: var(--primary-color);
                        text-decoration: none;
                        font-size: 13px;
                    }

                    /* Summary 
                    .summary-content {
                        font-size: 14px;
                        line-height: 1.6;
                        color: #4b5563;
                    }

                    .summary-content p {
                        margin-bottom: 12px;
                    }

                    .summary-content ul {
                        margin: 12px 0;
                        padding-left: 24px;
                    }

                    .summary-content li {
                        margin-bottom: 6px;
                    }

                    .summary-content strong {
                        color: #1f2937;
                    }
                    */
                    /* Experience */
                    .exp-row {
                        display: flex;
                        justify-content: space-between;
                        gap: 12px;
                        margin-bottom: 2px;
                    }

                    .exp-description {
                        margin-top: 8px;
                        font-size: 14px;
                        color: #4b5563;
                    }

                    /* Education */
                    .edu-row {
                        display: flex;
                        justify-content: space-between;
                        gap: 12px;
                        margin-bottom: 2px;
                    }

                    /* Utilities */
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

                    .shrink-0 {
                        flex-shrink: 0;
                    }

                    .mt-2 {
                        margin-top: 2px;
                    }

                    .mb-2 {
                        margin-bottom: 8px;
                    }

                    .gap-x-2 {
                        column-gap: 8px;
                    }
                    
                    .flex {
                        display: flex;
                        justify-content: space-between;
                        gap: 12px;
                        margin-bottom: 2px;                      
                    }                                      

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
        <div class="resume-preview-container">                            
            <!-- Header -->
            <div class="page-header">
                <div class="header-main">
                    <div class="profile-picture">
                        <xsl:choose>
                            <xsl:when test="picturePath != '' and picturePath != 'data:,'">
                                <img src="{picturePath}" alt="Profile picture"/>
                            </xsl:when>
                            <xsl:otherwise>
                                <svg viewBox="0 0 24 24" fill="currentColor">
                                    <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
                                </svg>
                            </xsl:otherwise>
                        </xsl:choose>
                    </div>
                    <div class="profile-info">
                        <h2 class="profile-name"><xsl:value-of select="name"/></h2>
                        <p class="profile-headline"><xsl:value-of select="headline"/></p>
                    </div>
                </div>
            </div>
            <!-- Contact Items -->
            <div class="contact-bar">
                <div class="contact-items">
                    <xsl:if test="email != ''">
                        <div class="contact-item">
                            <svg xmlns="http://www.w3.org/2000/svg" width="15.75" height="15.75" fill="var(--primary-color)" viewBox="0 0 256 256">
                                <path d="M224,48H32a8,8,0,0,0-8,8V192a16,16,0,0,0,16,16H216a16,16,0,0,0,16-16V56A8,8,0,0,0,224,48Zm-96,85.15L52.57,64H203.43ZM98.71,128,40,181.81V74.19Zm11.84,10.85,12,11.05a8,8,0,0,0,10.82,0l12-11.05,58,53.15H52.57ZM157.29,128,216,74.18V181.82Z"></path>
                            </svg>
                            <a href="mailto:{email}" target="_blank"><xsl:value-of select="email"/></a>
                        </div>
                    </xsl:if> 
                    <xsl:if test="phone != ''">
                        <div class="contact-item">
                            <svg xmlns="http://www.w3.org/2000/svg" width="15.75" height="15.75" fill="var(--primary-color)" viewBox="0 0 256 256">
                                <path d="M222.37,158.46l-47.11-21.11-.13-.06a16,16,0,0,0-15.17,1.4,8.12,8.12,0,0,0-.75.56L134.87,160c-15.42-7.49-31.34-23.29-38.83-38.51l20.78-24.71c.2-.25.39-.5.57-.77a16,16,0,0,0,1.32-15.06l0-.12L97.54,33.64a16,16,0,0,0-16.62-9.52A56.26,56.26,0,0,0,32,80c0,79.4,64.6,144,144,144a56.26,56.26,0,0,0,55.88-48.92A16,16,0,0,0,222.37,158.46ZM176,208A128.14,128.14,0,0,1,48,80a40.2,40.2,0,0,1,34.87-40,.61.61,0,0,0,0,.12l21,47L83.2,111.86a6.13,6.13,0,0,0-.57.77,16,16,0,0,0-1,15.7c9.06,18.53,27.73,37.06,46.46,46.11a16,16,0,0,0,15.75-1.14,8.44,8.44,0,0,0,.74-.56L168.89,152l47,21.05h0s.08,0,.11,0A40.21,40.21,0,0,1,176,208Z"></path>
                            </svg>
                            <a href="tel:{phone}" target="_blank"><xsl:value-of select="phone"/></a>
                        </div>
                    </xsl:if>
                    <xsl:if test="location != ''">
                        <div class="contact-item">
                            <svg xmlns="http://www.w3.org/2000/svg" width="15.75" height="15.75" fill="var(--primary-color)" viewBox="0 0 256 256">
                                <path d="M128,64a40,40,0,1,0,40,40A40,40,0,0,0,128,64Zm0,64a24,24,0,1,1,24-24A24,24,0,0,1,128,128Zm0-112a88.1,88.1,0,0,0-88,88c0,31.4,14.51,64.68,42,96.25a254.19,254.19,0,0,0,41.45,38.3,8,8,0,0,0,9.18,0A254.19,254.19,0,0,0,174,200.25c27.45-31.57,42-64.85,42-96.25A88.1,88.1,0,0,0,128,16Zm0,206c-16.53-13-72-60.75-72-118a72,72,0,0,1,144,0C200,161.23,144.53,209,128,222Z"></path>
                            </svg>
                            <span><xsl:value-of select="location"/></span>
                        </div>
                    </xsl:if> 
                    <xsl:apply-templates select="links/link"/>                                                          
                </div>
            </div> 
            <!-- Main Content with Sidebar  -->
            <div class="main-layout">
                <!-- Sidebar -->
                <div class="sidebar">
                    <!-- Skills -->
                    <xsl:if test="skills/skill">
                        <section class="page-section">
                            <h6 class="mb-1.5 text-(--primary-color)">Skills</h6>
                            <div class="section-content grid">
                                <xsl:apply-templates select="skills/skill"/>
                            </div>
                        </section>
                    </xsl:if>
                    
                    <!-- Certifications -->
                    <xsl:if test="certifications/certification">
                        <section class="page-section">
                            <h6 class="mb-1.5 text-(--primary-color)">Certifications</h6>
                            <div class="section-content grid">
                                <xsl:apply-templates select="certifications/certification"/>
                            </div>
                        </section>
                    </xsl:if>
                    
                    <!-- Languages -->
                    <xsl:if test="languages/language">
                        <section class="page-section">
                            <h6 class="mb-1.5 text-(--primary-color)">Languages</h6>
                            <div class="section-content grid">
                                <xsl:apply-templates select="languages/language"/>
                            </div>
                        </section>
                    </xsl:if>
                </div>
                <!-- Main Content -->
                <div class="content">
                    <!-- Summary -->
                    <xsl:if test="summary != ''">
                        <section class="page-section">
                            <h6 class="mb-1.5 text-(--primary-color)">Summary</h6>
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
                            <h6 class="mb-1.5 text-(--primary-color)">Experience</h6>
                            <div class="section-content grid">
                                <xsl:apply-templates select="experiences/experience"/>
                            </div>
                        </section>
                    </xsl:if>
                    
                    <!-- Education -->
                    <xsl:if test="educations/education">
                        <section class="page-section">
                            <h6 class="mb-1.5 text-(--primary-color)">Education</h6>
                            <div class="section-content grid">
                                <xsl:apply-templates select="educations/education"/>
                            </div>
                        </section>
                    </xsl:if>
                </div>
            </div>                                                                                  
        </div>
    </xsl:template>
    
    <xsl:template match="link">
        <div class="contact-item">
            <svg xmlns="http://www.w3.org/2000/svg" width="15.75" height="15.75" fill="var(--primary-color)" viewBox="0 0 256 256">
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
    </xsl:template>


    <xsl:template match="skill">
        <div class="section-item">
            <div class="skills-item">
                <div class="section-item-header flex flex-col">
                    <strong class="section-item-title"><xsl:value-of select="name"/></strong>
                </div>
                <xsl:if test="keywords/keyword">
                    <div class="section-item-keywords opacity-80">
                        <xsl:apply-templates select="keywords/keyword"/>
                    </div>
                </xsl:if>
            </div>
        </div>
    </xsl:template>

    <xsl:template match="keyword">
        <span>
            <xsl:value-of select="."/>
            <xsl:if test="position() != last()">, </xsl:if>        
        </span>
    </xsl:template>

    <xsl:template match="certification">
        <div class="section-item">
            <div class="certifications-item">
                <div class="section-item-header">
                    <div class="flex gap-x-2">
                        <strong class="section-item-title"><xsl:value-of select="title"/></strong>
                        <xsl:if test="date != ''">
                            <span class="section-item-metadata shrink-0 text-end"><xsl:value-of select="date"/></span>
                        </xsl:if>
                    </div>
                    <xsl:if test="issuer != ''">
                        <div class="flex gap-x-2">
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
                    <div class="flex gap-x-2">
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
                    <div class="flex gap-x-2">
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
                    <div class="flex gap-x-2">
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
                    <div class="flex gap-x-2">
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