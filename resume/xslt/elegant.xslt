<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:ext="urn:ExtObj">
    <xsl:output method="html" indent="yes" encoding="UTF-8"/>

    <xsl:template match="/">
        <html lang="en">
            <head>
                <meta charset="UTF-8"/>
                <title>
                    <xsl:value-of select="resume/name"/>- CV
                </title>
                <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&amp;display=swap" rel="stylesheet"/>
                <style>
                    <![CDATA[
                    * { margin: 0; padding: 0; box-sizing: border-box; }
                    body { background: #fafafa; font-family: 'Inter', sans-serif; color: #1e293b; padding: 40px 20px; font-size: 14px; }
                    .mx-auto { max-width: 210mm; margin: 0 auto; }
                    .paper { background: white; border-radius: 20px; box-shadow: 0 20px 35px -12px rgba(0,0,0,0.1); overflow: hidden; }
                    .top-bar { background-color: darkslateblue; color: white; padding: 30px 40px; display: flex; gap: 30px; flex-wrap: wrap; align-items: center; }
                    .photo img, .photo { width: 120px; height: 120px; border-radius: 60px; object-fit: cover; background: white; }
                    .name h1 { font-size: 28px; font-weight: 600; }
                    .headline { color: #94a3b8; margin: 8px 0 12px; font-size: 15px; }
                    .details { display: flex; flex-wrap: wrap; gap: 20px; font-size: 12px; color: #cbd5e1; margin-bottom: 8px; }
                    .details a { color: #cbd5e1;; text-decoration: none;  }
                    .body { padding: 35px 40px; display: grid; grid-template-columns: 1fr 1.8fr; gap: 40px; }
                    .sidebar-section { margin-bottom: 30px; }
                    .sidebar-section h3 { color: #0f172a; font-size: 14px; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 15px; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px; }
                    .skill-item { margin-bottom: 16px; }
                    .skill-name { font-weight: 600; font-size: 13px; margin-bottom: 6px; color: #334155; }
                    .skill-list { display: flex; flex-wrap: wrap; gap: 5px; }
                    .skill-list span { background: #f1f5f9; padding: 4px 12px; border-radius: 30px; font-size: 11px; }
                    .lang { margin-bottom: 12px; }
                    .language-level { display: flex; gap: 6px; margin-top: 5px; }
                    .level-dot { width: 10px; height: 10px; border: 1px solid darkslateblue; border-radius: 50%; }
                    .level-dot.active { background: darkslateblue; }
                    .main-section { margin-bottom: 28px; }
                    .main-section h3 { color: #0f172a; font-size: 16px; border-left: 4px solid darkslateblue; padding-left: 12px; margin-bottom: 15px; }
                    .job { margin-bottom: 22px; }
                    .job-header { display: flex; justify-content: space-between; flex-wrap: wrap; margin-bottom: 4px; }
                    .job-title { font-weight: 700; }
                    .job-period { font-size: 11px; color: #64748b; }
                    .job-company { color: darkslateblue; font-weight: 500; font-size: 13px; margin-bottom: 6px; }
                    .job-company  a { color: darkslateblue; text-decoration: none; }
                    .job-desc { font-size: 12px; color: #334155; margin-top: 6px; padding-left: 24px; }
                    .cert, .edu { font-size: 12px; margin-bottom: 12px; }
                    .cert a { color: #1e293b; text-decoration: none; }
		            .edu a { color: #1e293b; text-decoration: none; }
                    @media (max-width: 750px) { .body { grid-template-columns: 1fr; gap: 20px; } .top-bar { flex-direction: column; text-align: center; } }
                    @media print { body { padding: 0; background: white; } .paper { box-shadow: none; } }
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
        <div class="paper">
            <div class="top-bar">
                <div class="photo">
                    <xsl:choose>
                        <xsl:when test="picture != '' and picture != 'data:,'">
                            <div class="photo" style="background:url('{picture}') no-repeat center; background-size: cover;"></div>
                        </xsl:when>
                        <xsl:otherwise>
                            <svg viewBox="0 0 24 24" fill="currentColor" width="120" height="120">
                                <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
                            </svg>
                        </xsl:otherwise>
                    </xsl:choose>
                </div>
                <div class="name">
                    <h1>
                        <xsl:value-of select="name"/>
                    </h1>
                    <div class="headline">
                        <xsl:value-of select="headline"/>
                    </div>
                    <div class="details">
                        <span>✉️<a href="mailto:{email}" target="_blank"><xsl:value-of select="email"/></a></span>
                        <span>📱<a href="tel:{phone}" target="_blank"><xsl:value-of select="phone"/></a></span>
                        <span>📍<xsl:value-of select="location"/></span>
                    </div>
                    <div class="details">
                        <xsl:for-each select="links/link">
                            <span>🌐
                                <a href="{.}" target="_blank">
                                    <xsl:choose>
                                        <xsl:when test="contains(., 'linkedin')">LinkedIn</xsl:when>
                                        <xsl:when test="contains(., 'github')">GitHub</xsl:when>
                                        <xsl:otherwise><xsl:value-of select="."/></xsl:otherwise>
                                    </xsl:choose>
                                </a>
                            </span>
                        </xsl:for-each>
                    </div>                    
                </div>
            </div>
            <div class="body">
                <div class="sidebar">
                    <div class="sidebar-section">
                        <h3>Technical Skills</h3>
                        <xsl:for-each select="skills/skill">
                            <div class="skill-item">
                                <div class="skill-name">
                                    <xsl:value-of select="name"/>
                                </div>
                                <div class="skill-list">
                                    <xsl:for-each select="keywords/keyword">
                                        <span>
                                            <xsl:value-of select="."/>
                                        </span>
                                    </xsl:for-each>
                                </div>
                            </div>
                        </xsl:for-each>
                    </div>
                    <div class="sidebar-section">
                        <h3>Certifications</h3>
                        <xsl:for-each select="certifications/certification">
                            <div class="cert">
                                <strong>
                                    <xsl:value-of select="title"/>
                                </strong>
                                <br/>
                                <xsl:value-of select="issuer"/>
                             ·  <xsl:value-of select="date"/>
                                <xsl:if test="website != ''">
                                    <a href="{website}" target="_blank" rel="noopener">
                                     ·  <xsl:value-of select="label"/>
                                    </a>
                                </xsl:if>                              
                            </div>
                        </xsl:for-each>
                    </div>
                    <div class="sidebar-section">
                        <h3>Languages</h3>
                        <xsl:for-each select="languages/language">
                            <div class="lang">
                                <strong>
                                    <xsl:value-of select="name"/>
                                </strong>
                                <xsl:if test="fluency != ''">(<xsl:value-of select="fluency"/>)</xsl:if>
                                <div class="language-level">
                                    <xsl:call-template name="level">
                                        <xsl:with-param name="lvl" select="level"/>
                                    </xsl:call-template>
                                </div>
                            </div>
                        </xsl:for-each>
                    </div>
                </div>
                <div class="main">
                    <div class="main-section">
                        <h3>Professional Summary</h3>
                        <div style="padding-left: 24px;">
                            <xsl:value-of select="ext:ConvertToHtml(summary)" disable-output-escaping="yes"/>
                        </div>
                    </div>
                    <div class="main-section">
                        <h3>Work Experience</h3>
                        <xsl:for-each select="experiences/experience">
                            <div class="job">
                                <div class="job-header">
                                    <span class="job-title">
                                        <xsl:value-of select="position"/>
                                    </span>
                                    <span class="job-period">
                                        <xsl:value-of select="period"/>
                                    </span>
                                </div>
                                <div class="job-company">
                                    <xsl:choose>
                                        <xsl:when test="website != ''">
                                            <a href="{website}" target="_blank" rel="noopener" >
                                                <b><xsl:value-of select="company"/></b>
                                            </a>
                                        </xsl:when>
                                        <xsl:otherwise>
                                            <b class="section-item-title"><xsl:value-of select="company"/></b>
                                        </xsl:otherwise>
                                    </xsl:choose>
                                 |  <xsl:value-of select="location"/>
                                </div>
                                <div class="job-desc">
                                    <xsl:value-of select="ext:ConvertToHtml(description)" disable-output-escaping="yes"/>
                                </div>
                            </div>
                        </xsl:for-each>
                    </div>
                    <div class="main-section">
                        <h3>Education</h3>
                        <xsl:for-each select="educations/education">
                            <div class="edu">
                                <b>
                                    <xsl:value-of select="degree"/>
                                </b> in <xsl:value-of select="area"/>
                            <br/>
                            <xsl:choose>
                                <xsl:when test="website != ''">
                                    <a href="{website}" target="_blank" rel="noopener">
                                        <b><xsl:value-of select="school"/></b>
                                    </a>
                                </xsl:when>
                                <xsl:otherwise>
                                    <b><xsl:value-of select="school"/></b>
                                </xsl:otherwise>
                            </xsl:choose>
                         ·  <xsl:value-of select="period"/>
                            <br/>
                            <xsl:value-of select="location"/>
                            <xsl:if test="grade != ''">
                                <br/>
                            Grade: <xsl:value-of select="grade"/>
                            </xsl:if>
                        </div>
                    </xsl:for-each>
                </div>
            </div>
        </div>
    </div>
</xsl:template>

<xsl:template name="level">
    <xsl:param name="lvl"/>
    <div class="level-dot">
        <xsl:if test="$lvl &gt;= 1">
            <xsl:attribute name="class">level-dot active</xsl:attribute>
        </xsl:if>
    </div>
    <div class="level-dot">
        <xsl:if test="$lvl &gt;= 2">
            <xsl:attribute name="class">level-dot active</xsl:attribute>
        </xsl:if>
    </div>
    <div class="level-dot">
        <xsl:if test="$lvl &gt;= 3">
            <xsl:attribute name="class">level-dot active</xsl:attribute>
        </xsl:if>
    </div>
    <div class="level-dot">
        <xsl:if test="$lvl &gt;= 4">
            <xsl:attribute name="class">level-dot active</xsl:attribute>
        </xsl:if>
    </div>
    <div class="level-dot">
        <xsl:if test="$lvl &gt;= 5">
            <xsl:attribute name="class">level-dot active</xsl:attribute>
        </xsl:if>
    </div>
</xsl:template>
</xsl:stylesheet>